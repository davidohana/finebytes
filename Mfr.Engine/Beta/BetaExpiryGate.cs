using System.Diagnostics;
using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.Beta
{
    /// <summary>
    /// Process-wide beta expiry check preferring a simple HTTPS <c>Date</c> header, else local UTC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always compiled so unit tests can inject clock and network without <c>-p:BETA=true</c>.
    /// Commit enforcement is active in <c>BETA</c> builds; non-BETA builds stay inactive unless
    /// tests opt in via <see cref="SetEnforceForTests"/>.
    /// </para>
    /// </remarks>
    public static class BetaExpiryGate
    {
        /// <summary>
        /// UTC instant at which the beta becomes expired (inclusive). Last valid moment is end of 2026-12-31 UTC.
        /// </summary>
        public static readonly DateTime ExpiresUtc = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

        private static readonly Lock Sync = new();
        private static Func<DateTime> _utcNow = static () => DateTime.UtcNow;
        private static Func<CancellationToken, DateTime?>? _tryFetchNetworkUtc;
        private static bool _probeCompleted;
        private static DateTime? _cachedNetworkUtc;
        private static Stopwatch? _sinceNetworkProbe;
#if !BETA
        private static bool EnforceForTests { get; set; }
#endif

        /// <summary>
        /// Gets whether commit enforcement is active for this process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>true</c> in <c>BETA</c> builds. In non-BETA builds, <c>false</c> unless tests call
        /// <see cref="SetEnforceForTests"/>.
        /// </para>
        /// </remarks>
        public static bool IsEnforcementEnabled =>
#if BETA
            true;
#else
            EnforceForTests;
#endif

        /// <summary>
        /// Gets whether the effective UTC clock is on or after <see cref="ExpiresUtc"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Starts the one-shot network probe when it has not run yet (may block briefly). Prefer
        /// <see cref="IsExpiredWithoutProbe"/> on UI CanExecute paths.
        /// </para>
        /// </remarks>
        public static bool IsExpired => GetEffectiveUtc() >= ExpiresUtc;

        /// <summary>
        /// Gets whether expiry holds using only cached network time or local UTC — never starts a probe.
        /// </summary>
        public static bool IsExpiredWithoutProbe => _GetEffectiveUtcWithoutProbe() >= ExpiresUtc;

        /// <summary>
        /// Returns the effective UTC used for expiry: cached network time advanced by local elapsed, or local UTC.
        /// </summary>
        /// <returns>UTC instant used for the expiry comparison.</returns>
        public static DateTime GetEffectiveUtc()
        {
            _EnsureNetworkProbe();
            return _GetEffectiveUtcWithoutProbe();
        }

        /// <summary>
        /// Cached network UTC advanced by local elapsed, or local UTC, without starting a probe.
        /// </summary>
        private static DateTime _GetEffectiveUtcWithoutProbe()
        {
            lock (Sync)
            {
                if (_cachedNetworkUtc is { } networkUtc && _sinceNetworkProbe is not null)
                {
                    return networkUtc + _sinceNetworkProbe.Elapsed;
                }

                return _utcNow();
            }
        }

        /// <summary>
        /// Performs the one-shot HTTPS <c>Date</c> probe if it has not run yet for this process.
        /// </summary>
        public static void TryRefreshNetworkUtc()
        {
            _EnsureNetworkProbe();
        }

        /// <summary>
        /// Throws <see cref="BetaExpiredException"/> when a non-dry-run commit is disallowed.
        /// </summary>
        /// <param name="dryRun">When <see langword="true"/>, never throws.</param>
        /// <exception cref="BetaExpiredException">Thrown when enforcement is on and the beta has expired.</exception>
        public static void ThrowIfCommitDisallowed(bool dryRun)
        {
            if (dryRun)
            {
                return;
            }

            if (!IsEnforcementEnabled)
            {
                return;
            }

            if (!IsExpired)
            {
                return;
            }

            throw new BetaExpiredException(ExpiresUtc);
        }

        /// <summary>
        /// Test hook: replaces local clock and/or network fetch, and clears the process probe cache.
        /// </summary>
        /// <param name="utcNow">Local UTC provider; <see langword="null"/> restores <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="tryFetchNetworkUtc">
        /// Optional network UTC fetch. Return a UTC instant on success, or <see langword="null"/> on failure.
        /// Pass <see langword="null"/> to use the real HTTPS probe.
        /// </param>
        internal static void ConfigureForTests(
            Func<DateTime>? utcNow = null,
            Func<CancellationToken, DateTime?>? tryFetchNetworkUtc = null
        )
        {
            lock (Sync)
            {
                _utcNow = utcNow is null ? static () => DateTime.UtcNow : utcNow;
                _tryFetchNetworkUtc = tryFetchNetworkUtc;
                _ClearProbeStateUnlocked();
            }
        }

        /// <summary>
        /// Test hook: restores default clock/network and clears probe cache and non-BETA enforce flag.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (Sync)
            {
                _utcNow = static () => DateTime.UtcNow;
                _tryFetchNetworkUtc = null;
                _ClearProbeStateUnlocked();
#if !BETA
                EnforceForTests = false;
#endif
            }
        }

        /// <summary>
        /// Test hook: enables commit enforcement in non-BETA builds so Commit expiry can be asserted locally.
        /// </summary>
        /// <param name="enforce">When <see langword="true"/>, non-dry-run commits throw if expired.</param>
        internal static void SetEnforceForTests(bool enforce)
        {
#if BETA
            _ = enforce;
#else
            lock (Sync)
            {
                EnforceForTests = enforce;
            }
#endif
        }

        /// <summary>
        /// Runs the one-shot network probe outside the lock; caches success or falls back to local UTC.
        /// </summary>
        private static void _EnsureNetworkProbe()
        {
            lock (Sync)
            {
                if (_probeCompleted)
                {
                    return;
                }
            }

            DateTime? networkUtc = null;
            try
            {
                using var cts = new CancellationTokenSource(ProbeTimeout);
                networkUtc = _FetchNetworkUtc(cts.Token);
            }
            catch (Exception ex)
            {
                // Offline, timeout, or bad response: fall back to local UTC.
                Log.Debug(ex, "Beta network time probe failed; using local UTC.");
                networkUtc = null;
            }

            lock (Sync)
            {
                // Concurrent first callers may both fetch; keep the first success (or a late success
                // if an earlier failure marked the probe complete with an empty cache).
                var alreadyCompleted = _probeCompleted;
                var hadCachedNetworkUtc = _cachedNetworkUtc is not null;

                if (networkUtc is { } utc && _cachedNetworkUtc is null)
                {
                    _cachedNetworkUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
                    _sinceNetworkProbe = Stopwatch.StartNew();
                    Log.Debug(
                        "Beta network time probe succeeded from {ProbeUrl}: {NetworkUtc:O} UTC.",
                        ProductUrls.WebSite,
                        _cachedNetworkUtc
                    );
                }
                else if (!alreadyCompleted && !hadCachedNetworkUtc)
                {
                    Log.Debug(
                        "Beta network time probe unavailable from {ProbeUrl}; using local UTC {LocalUtc:O}.",
                        ProductUrls.WebSite,
                        _utcNow()
                    );
                }

                _probeCompleted = true;
            }
        }

        /// <summary>
        /// Returns network UTC from the test override when set; otherwise the HTTPS <c>Date</c> probe.
        /// </summary>
        /// <param name="cancellationToken">Token that cancels the probe.</param>
        /// <returns>UTC instant on success; <see langword="null"/> when unavailable.</returns>
        private static DateTime? _FetchNetworkUtc(CancellationToken cancellationToken)
        {
            Func<CancellationToken, DateTime?>? overrideFetch;
            lock (Sync)
            {
                overrideFetch = _tryFetchNetworkUtc;
            }

            if (overrideFetch is not null)
            {
                return overrideFetch(cancellationToken);
            }

            return _FetchNetworkUtcViaHttp(cancellationToken);
        }

        /// <summary>
        /// HEAD-probes the product site and reads the response <c>Date</c> header as UTC.
        /// </summary>
        /// <param name="cancellationToken">Token that cancels the HTTP send.</param>
        /// <returns>UTC from <c>Date</c>, or <see langword="null"/> when the header is missing.</returns>
        private static DateTime? _FetchNetworkUtcViaHttp(CancellationToken cancellationToken)
        {
            using var client = new HttpClient { Timeout = ProbeTimeout };
            using var request = new HttpRequestMessage(HttpMethod.Head, ProductUrls.WebSite);
            using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.Headers.Date is not { } date)
            {
                return null;
            }

            return date.UtcDateTime;
        }

        /// <summary>
        /// Clears cached network time so the next probe runs again (caller must hold <see cref="Sync"/>).
        /// </summary>
        private static void _ClearProbeStateUnlocked()
        {
            _probeCompleted = false;
            _cachedNetworkUtc = null;
            _sinceNetworkProbe = null;
        }
    }
}
