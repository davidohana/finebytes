using System.Globalization;
using System.Net;
using System.Xml.Linq;

namespace Mfr.Metadata.GeoNames
{
    /// <summary>
    /// HTTPS GeoNames <c>findNearby</c> client with L1-facing lookups via L2/L3 cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bundled default username is <see cref="DefaultUsername"/>. Prefer
    /// <see cref="UsernameOverrideProvider"/> (wired from Options) when non-blank.
    /// Failures throw <see cref="InvalidOperationException"/> (rate limits use
    /// <see cref="GeoNamesRateLimitException"/>). Failures are never written to L3.
    /// </para>
    /// </remarks>
    public sealed class GeoNamesClient : IDisposable
    {
        /// <summary>
        /// Bundled FineBytes GeoNames username used when Options override is blank.
        /// </summary>
        public const string DefaultUsername = "fbmfr";

        private static readonly Lock s_SharedGate = new();
        private static GeoNamesClient? s_Shared;

        private readonly HttpClient _http;
        private readonly GeoNamesResponseCache _cache;

        /// <summary>
        /// Optional Options override provider. Return blank/null to use <see cref="DefaultUsername"/>.
        /// </summary>
        public static Func<string>? UsernameOverrideProvider { get; set; }

        /// <summary>
        /// Test hook replacing the default <see cref="HttpClient"/> handler for the shared instance.
        /// </summary>
        public static HttpMessageHandler? SharedHttpHandlerOverride { get; set; }

        /// <summary>
        /// Test hook replacing the default L3 cache path for the shared instance.
        /// </summary>
        public static string? SharedCacheFilePathOverride { get; set; }

        /// <summary>
        /// Initializes a client with optional handler and cache path (tests).
        /// </summary>
        /// <param name="handler">Optional message handler; when null, a default handler is used.</param>
        /// <param name="cacheFilePath">Optional L3 path; when null, uses <see cref="GeoNamesResponseCache.DefaultCacheFilePath"/>.</param>
        public GeoNamesClient(HttpMessageHandler? handler = null, string? cacheFilePath = null)
        {
            _http = handler is null
                ? new HttpClient { Timeout = TimeSpan.FromSeconds(10) }
                : new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(10) };
            _cache = new GeoNamesResponseCache(cacheFilePath ?? GeoNamesResponseCache.DefaultCacheFilePath);
        }

        /// <summary>
        /// Process-wide shared client (preview / Rename List path).
        /// </summary>
        public static GeoNamesClient Shared
        {
            get
            {
                lock (s_SharedGate)
                {
                    return s_Shared ??= new GeoNamesClient(SharedHttpHandlerOverride, SharedCacheFilePathOverride);
                }
            }
        }

        /// <summary>
        /// Replaces the shared client (tests). Pass <see langword="null"/> to recreate on next access.
        /// </summary>
        /// <param name="client">Replacement client, or <see langword="null"/>.</param>
        public static void SetSharedForTests(GeoNamesClient? client)
        {
            lock (s_SharedGate)
            {
                s_Shared?.Dispose();
                s_Shared = client;
            }
        }

        /// <summary>
        /// Resolves the effective GeoNames username (override wins when non-blank).
        /// </summary>
        /// <param name="overrideUsername">Options override, or <see langword="null"/>/blank for bundled.</param>
        /// <returns>Trimmed username used in HTTP and cache keys.</returns>
        public static string ResolveEffectiveUsername(string? overrideUsername)
        {
            if (string.IsNullOrWhiteSpace(overrideUsername))
            {
                return DefaultUsername;
            }

            return overrideUsername.Trim();
        }

        /// <summary>
        /// Resolves the effective username from <see cref="UsernameOverrideProvider"/>.
        /// </summary>
        /// <returns>Trimmed username used in HTTP and cache keys.</returns>
        public static string EffectiveUsername()
        {
            return ResolveEffectiveUsername(UsernameOverrideProvider?.Invoke());
        }

        /// <summary>
        /// Clears L2 process cache and circuit breaker on the shared client (not L3).
        /// </summary>
        public static void ClearSharedProcessState()
        {
            Shared.ClearProcessState();
        }

        /// <summary>
        /// Clears L2 process cache and circuit breaker for this instance (not L3).
        /// </summary>
        public void ClearProcessState()
        {
            _cache.ClearProcessState();
        }

        /// <summary>
        /// Flushes pending L3 <c>LastAccessUtc</c> updates on the shared client when it exists.
        /// </summary>
        /// <remarks>
        /// Does not create the shared instance. Hosts call this at process exit.
        /// </remarks>
        public static void FlushSharedDiskCache()
        {
            lock (s_SharedGate)
            {
                s_Shared?.FlushDiskCache();
            }
        }

        /// <summary>
        /// Writes pending L3 <c>LastAccessUtc</c> updates to disk when dirty.
        /// </summary>
        public void FlushDiskCache()
        {
            _cache.Flush();
        }

        /// <summary>
        /// Looks up nearby place/region/country for GPS coordinates (sync; uses L2/L3 then HTTP).
        /// </summary>
        /// <param name="latitude">GPS latitude in decimal degrees.</param>
        /// <param name="longitude">GPS longitude in decimal degrees.</param>
        /// <param name="username">Effective username; when null, uses <see cref="EffectiveUsername"/>.</param>
        /// <returns>Successful snapshot (fields may be blank).</returns>
        /// <exception cref="GeoNamesRateLimitException">Quota/rate-limit (trips breaker).</exception>
        /// <exception cref="InvalidOperationException">Network/HTTP/XML failure.</exception>
        public GeoNamesInfo FindNearby(double latitude, double longitude, string? username = null)
        {
            var effectiveUsername = username is null ? EffectiveUsername() : ResolveEffectiveUsername(username);
            var cacheKey = GeoNamesCacheKey.Create(latitude, longitude, effectiveUsername);

            if (_cache.TryGet(cacheKey, out var cached))
            {
                return cached;
            }

            if (_cache.TryGetTrippedMessage(effectiveUsername, out var trippedMessage))
            {
                throw new GeoNamesRateLimitException(trippedMessage);
            }

            try
            {
                var info = _FetchFindNearby(latitude, longitude, effectiveUsername);
                _cache.Put(cacheKey, info);
                return info;
            }
            catch (GeoNamesRateLimitException ex)
            {
                _cache.Trip(effectiveUsername, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Parses a GeoNames <c>findNearby</c> XML body into a snapshot.
        /// </summary>
        /// <param name="xml">Response XML text.</param>
        /// <returns>Successful snapshot (empty when no <c>geoname</c>).</returns>
        /// <exception cref="GeoNamesRateLimitException">Status payload indicates quota/rate limit.</exception>
        /// <exception cref="InvalidOperationException">Malformed XML or other service status.</exception>
        public static GeoNamesInfo ParseFindNearbyXml(string xml)
        {
            ArgumentNullException.ThrowIfNull(xml);

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException)
            {
                throw new InvalidOperationException("GeoNames returned invalid XML.", ex);
            }

            var status = doc.Root?.Element("status");
            if (status is not null)
            {
                var message = ((string?)status.Attribute("message"))?.Trim();
                if (string.IsNullOrEmpty(message))
                {
                    message = status.Value.Trim();
                }

                if (string.IsNullOrEmpty(message))
                {
                    message = "GeoNames request failed.";
                }

                message = _TruncateMessage(message);
                if (_LooksLikeRateLimit(message))
                {
                    throw new GeoNamesRateLimitException(_RateLimitUserMessage(message));
                }

                throw new InvalidOperationException(message);
            }

            var geoname = doc.Root?.Element("geoname");
            if (geoname is null)
            {
                return new GeoNamesInfo();
            }

            return new GeoNamesInfo
            {
                Place = _NullIfBlank(geoname.Element("name")?.Value),
                Region = _NullIfBlank(geoname.Element("adminName1")?.Value),
                Country = _NullIfBlank(geoname.Element("countryName")?.Value),
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cache.Flush();
            _http.Dispose();
        }

        private GeoNamesInfo _FetchFindNearby(double latitude, double longitude, string effectiveUsername)
        {
            var url =
                "https://api.geonames.org/findNearby?lat="
                + latitude.ToString(CultureInfo.InvariantCulture)
                + "&lng="
                + longitude.ToString(CultureInfo.InvariantCulture)
                + "&style=FULL&username="
                + Uri.EscapeDataString(effectiveUsername);

            HttpResponseMessage response;
            try
            {
                response = _http.GetAsync(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
                when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                throw new InvalidOperationException("GeoNames request failed.", ex);
            }

            using (response)
            {
                string body;
                try
                {
                    body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                    when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
                {
                    throw new InvalidOperationException("GeoNames request failed.", ex);
                }

                // Do not scan the full success body for rate-limit phrases — place names like
                // "Crediton" contain "credit" and would false-trip the breaker. Status XML and
                // HTTP 429 are the authoritative signals; ParseFindNearbyXml handles status nodes.
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var message = _RateLimitUserMessage(_ExtractStatusMessage(body) ?? body);
                    throw new GeoNamesRateLimitException(message);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var detail = _ExtractStatusMessage(body);
                    if (!string.IsNullOrEmpty(detail) && _LooksLikeRateLimit(detail))
                    {
                        throw new GeoNamesRateLimitException(_RateLimitUserMessage(detail));
                    }

                    var statusText = string.IsNullOrEmpty(detail)
                        ? $"GeoNames HTTP {(int)response.StatusCode}."
                        : detail;
                    throw new InvalidOperationException(_TruncateMessage(statusText));
                }

                return ParseFindNearbyXml(body);
            }
        }

        private static string? _ExtractStatusMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body.IndexOf("<status", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            try
            {
                var doc = XDocument.Parse(body);
                var status = doc.Root?.Element("status");
                if (status is null)
                {
                    return null;
                }

                var message = ((string?)status.Attribute("message"))?.Trim();
                return string.IsNullOrEmpty(message) ? status.Value.Trim() : message;
            }
            catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException)
            {
                return null;
            }
        }

        private static bool _LooksLikeRateLimit(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Contains("hourly limit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("daily limit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("credit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("quota", StringComparison.OrdinalIgnoreCase)
                || text.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
        }

        private static string _RateLimitUserMessage(string detail)
        {
            var truncated = _TruncateMessage(detail);
            if (truncated.StartsWith("GeoNames rate limit", StringComparison.OrdinalIgnoreCase))
            {
                return truncated;
            }

            return "GeoNames rate limit exceeded. " + truncated;
        }

        private static string _TruncateMessage(string message)
        {
            const int max = 240;
            var trimmed = message.Trim();
            if (trimmed.Length <= max)
            {
                return trimmed;
            }

            return trimmed[..max] + "…";
        }

        private static string? _NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
