using System.Net;
using System.Text;
using Mfr.Metadata.GeoNames;

namespace Mfr.Tests.Metadata.GeoNames
{
    /// <summary>
    /// Unit tests for GeoNames XML parse, HTTP client, L2/L3 cache, and circuit breaker.
    /// </summary>
    public sealed class GeoNamesClientTests : IDisposable
    {
        private readonly string _cacheDir;
        private readonly string _cachePath;
        private int _httpCalls;

        public GeoNamesClientTests()
        {
            _cacheDir = Path.Combine(Path.GetTempPath(), "mfr-geonames-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_cacheDir);
            _cachePath = Path.Combine(_cacheDir, "geonames-cache.json");
            GeoNamesClient.SetSharedForTests(null);
            GeoNamesClient.SharedHttpHandlerOverride = null;
            GeoNamesClient.SharedCacheFilePathOverride = null;
            GeoNamesClient.UsernameOverrideProvider = null;
        }

        public void Dispose()
        {
            GeoNamesClient.SetSharedForTests(null);
            GeoNamesClient.SharedHttpHandlerOverride = null;
            GeoNamesClient.SharedCacheFilePathOverride = null;
            GeoNamesClient.UsernameOverrideProvider = null;
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    Directory.Delete(_cacheDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // best-effort temp cleanup
            }
        }

        [Fact]
        public void ResolveEffectiveUsername_blank_uses_fbmfr()
        {
            Assert.Equal(GeoNamesClient.DefaultUsername, GeoNamesClient.ResolveEffectiveUsername(null));
            Assert.Equal(GeoNamesClient.DefaultUsername, GeoNamesClient.ResolveEffectiveUsername(""));
            Assert.Equal(GeoNamesClient.DefaultUsername, GeoNamesClient.ResolveEffectiveUsername("   "));
        }

        [Fact]
        public void ResolveEffectiveUsername_override_wins_when_non_blank()
        {
            Assert.Equal("myuser", GeoNamesClient.ResolveEffectiveUsername(" myuser "));
        }

        [Fact]
        public void ParseFindNearbyXml_reads_first_geoname()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames>
                  <geoname>
                    <name>Haifa</name>
                    <adminName1>Haifa District</adminName1>
                    <countryName>Israel</countryName>
                  </geoname>
                </geonames>
                """;

            var info = GeoNamesClient.ParseFindNearbyXml(xml);

            Assert.Equal("Haifa", info.Place);
            Assert.Equal("Haifa District", info.Region);
            Assert.Equal("Israel", info.Country);
        }

        [Fact]
        public void ParseFindNearbyXml_empty_geonames_returns_empty_snapshot()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames></geonames>
                """;

            var info = GeoNamesClient.ParseFindNearbyXml(xml);

            Assert.Null(info.Place);
            Assert.Null(info.Region);
            Assert.Null(info.Country);
        }

        [Fact]
        public void ParseFindNearbyXml_rate_limit_status_throws()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames>
                  <status message="the hourly limit of 1000 credits for fbmfr has been exceeded" value="18"/>
                </geonames>
                """;

            var ex = Assert.Throws<GeoNamesRateLimitException>(() => GeoNamesClient.ParseFindNearbyXml(xml));
            Assert.Contains("rate limit", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindNearby_place_name_containing_credit_is_not_rate_limit()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames>
                  <geoname>
                    <name>Crediton</name>
                    <adminName1>England</adminName1>
                    <countryName>United Kingdom</countryName>
                  </geoname>
                </geonames>
                """;
            using var client = _CreateClient(
                new StubHandler(_ =>
                {
                    Interlocked.Increment(ref _httpCalls);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xml, Encoding.UTF8, "application/xml"),
                    };
                })
            );

            var info = client.FindNearby(50.79, -3.65, "fbmfr");

            Assert.Equal("Crediton", info.Place);
            Assert.Equal(1, _httpCalls);
        }

        [Fact]
        public void FindNearby_cache_hit_skips_second_http()
        {
            using var client = _CreateClient(_SuccessHandler());

            var first = client.FindNearby(32.823057, 35.0444, "fbmfr");
            var second = client.FindNearby(32.823057, 35.0444, "fbmfr");

            Assert.Equal("Haifa", first.Place);
            Assert.Equal(first, second);
            Assert.Equal(1, _httpCalls);
        }

        [Fact]
        public void FindNearby_username_isolation_separate_cache_keys()
        {
            using var client = _CreateClient(_SuccessHandler());

            _ = client.FindNearby(32.823057, 35.0444, "fbmfr");
            _ = client.FindNearby(32.823057, 35.0444, "other");

            Assert.Equal(2, _httpCalls);
        }

        [Fact]
        public void FindNearby_rate_limit_trips_breaker_without_further_http()
        {
            using var client = _CreateClient(_RateLimitHandler());

            var first = Assert.Throws<GeoNamesRateLimitException>(() => client.FindNearby(10.0, 20.0, "fbmfr"));
            var second = Assert.Throws<GeoNamesRateLimitException>(() => client.FindNearby(11.0, 21.0, "fbmfr"));

            Assert.Contains("rate limit", first.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(first.Message, second.Message);
            Assert.Equal(1, _httpCalls);
        }

        [Fact]
        public void FindNearby_clear_process_state_allows_http_after_trip()
        {
            using var client = _CreateClient(_RateLimitHandler());

            Assert.Throws<GeoNamesRateLimitException>(() => client.FindNearby(10.0, 20.0, "fbmfr"));
            client.ClearProcessState();
            Assert.Throws<GeoNamesRateLimitException>(() => client.FindNearby(11.0, 21.0, "fbmfr"));

            Assert.Equal(2, _httpCalls);
        }

        [Fact]
        public void CacheKey_rounds_to_four_decimals()
        {
            var a = GeoNamesCacheKey.Create(32.823057, 35.0444, "fbmfr");
            var b = GeoNamesCacheKey.Create(32.8231, 35.0444, "fbmfr");
            Assert.Equal(a, b);
            Assert.Equal("32.8231|35.0444|fbmfr", a);
        }

        [Fact]
        public void L3_disk_hit_skips_http_on_new_client()
        {
            using (var first = _CreateClient(_SuccessHandler()))
            {
                _ = first.FindNearby(32.823057, 35.0444, "fbmfr");
            }

            Assert.Equal(1, _httpCalls);
            _httpCalls = 0;

            using var second = _CreateClient(_SuccessHandler());
            var info = second.FindNearby(32.823057, 35.0444, "fbmfr");

            Assert.Equal("Haifa", info.Place);
            Assert.Equal(0, _httpCalls);
        }

        private GeoNamesClient _CreateClient(HttpMessageHandler handler)
        {
            return new GeoNamesClient(handler, _cachePath);
        }

        private StubHandler _SuccessHandler()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames>
                  <geoname>
                    <name>Haifa</name>
                    <adminName1>Haifa District</adminName1>
                    <countryName>Israel</countryName>
                  </geoname>
                </geonames>
                """;
            return new StubHandler(_ =>
            {
                Interlocked.Increment(ref _httpCalls);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(xml, Encoding.UTF8, "application/xml"),
                };
            });
        }

        private StubHandler _RateLimitHandler()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <geonames>
                  <status message="the hourly limit of 1000 credits for fbmfr has been exceeded" value="18"/>
                </geonames>
                """;
            return new StubHandler(_ =>
            {
                Interlocked.Increment(ref _httpCalls);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(xml, Encoding.UTF8, "application/xml"),
                };
            });
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                return Task.FromResult(responder(request));
            }
        }
    }
}
