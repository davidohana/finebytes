using Mfr.Engine.Beta;

namespace Mfr.Tests.Engine.Beta
{
    /// <summary>
    /// Unit tests for <see cref="BetaExpiryGate"/> clock and network probe behavior.
    /// </summary>
    [Collection(BetaExpiryGateCollection.Name)]
    public sealed class BetaExpiryGateTests : IDisposable
    {
        /// <summary>
        /// Restores default gate state after each test.
        /// </summary>
        public void Dispose()
        {
            BetaExpiryGate.ResetForTests();
        }

        [Fact]
        public void ExpiresUtc_is_2027_01_01_UTC()
        {
            Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), BetaExpiryGate.ExpiresUtc);
        }

        [Fact]
        public void IsExpired_false_just_before_expiry_when_network_unavailable()
        {
            var justBefore = BetaExpiryGate.ExpiresUtc.AddTicks(-1);
            BetaExpiryGate.ConfigureForTests(utcNow: () => justBefore, tryFetchNetworkUtc: static _ => null);

            Assert.False(BetaExpiryGate.IsExpired);
            Assert.Equal(justBefore, BetaExpiryGate.GetEffectiveUtc());
        }

        [Fact]
        public void IsExpired_true_at_expiry_when_network_unavailable()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );

            Assert.True(BetaExpiryGate.IsExpired);
        }

        [Fact]
        public void IsExpired_uses_network_utc_when_probe_succeeds()
        {
            var localBefore = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            var networkAfter = new DateTime(2027, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            BetaExpiryGate.ConfigureForTests(utcNow: () => localBefore, tryFetchNetworkUtc: _ => networkAfter);

            Assert.True(BetaExpiryGate.IsExpired);
            Assert.True(BetaExpiryGate.GetEffectiveUtc() >= networkAfter);
        }

        [Fact]
        public void IsExpired_false_when_network_before_expiry_even_if_local_after()
        {
            var localAfter = new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var networkBefore = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            BetaExpiryGate.ConfigureForTests(utcNow: () => localAfter, tryFetchNetworkUtc: _ => networkBefore);

            Assert.False(BetaExpiryGate.IsExpired);
            Assert.True(BetaExpiryGate.GetEffectiveUtc() < BetaExpiryGate.ExpiresUtc);
        }

        [Fact]
        public void IsExpired_falls_back_to_local_when_probe_throws()
        {
            var localAfter = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => localAfter,
                tryFetchNetworkUtc: static _ => throw new HttpRequestException("offline")
            );

            Assert.True(BetaExpiryGate.IsExpired);
            Assert.Equal(localAfter, BetaExpiryGate.GetEffectiveUtc());
        }

        [Fact]
        public void Network_probe_runs_once_per_process_configuration()
        {
            var fetchCount = 0;
            var networkUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                tryFetchNetworkUtc: _ =>
                {
                    fetchCount++;
                    return networkUtc;
                }
            );

            _ = BetaExpiryGate.GetEffectiveUtc();
            _ = BetaExpiryGate.IsExpired;
            BetaExpiryGate.TryRefreshNetworkUtc();

            Assert.Equal(1, fetchCount);
        }

#if !BETA
        [Fact]
        public void ThrowIfCommitDisallowed_no_op_when_enforcement_off()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(false);

            BetaExpiryGate.ThrowIfCommitDisallowed(dryRun: false);
        }
#endif

        [Fact]
        public void ThrowIfCommitDisallowed_throws_when_enforced_and_expired()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(true);

            var ex = Assert.Throws<BetaExpiredException>(() => BetaExpiryGate.ThrowIfCommitDisallowed(dryRun: false));
            Assert.Equal(BetaExpiryGate.ExpiresUtc, ex.ExpiresUtc);
            Assert.Contains("2027-01-01", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ThrowIfCommitDisallowed_allows_dry_run_when_expired()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(true);

            BetaExpiryGate.ThrowIfCommitDisallowed(dryRun: true);
        }

        [Fact]
        public void IsExpiredWithoutProbe_uses_local_clock_without_fetch()
        {
            var fetchCalls = 0;
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: _ =>
                {
                    fetchCalls++;
                    return null;
                }
            );

            Assert.True(BetaExpiryGate.IsExpiredWithoutProbe);
            Assert.Equal(0, fetchCalls);
        }

        [Fact]
        public void FormatMessage_includes_expiry_day_and_download_guidance()
        {
            var message = BetaExpiredException.FormatMessage(BetaExpiryGate.ExpiresUtc);

            Assert.Contains("2027-01-01", message, StringComparison.Ordinal);
            Assert.Contains("Download a newer beta or the release.", message, StringComparison.Ordinal);
        }

#if !BETA
        [Fact]
        public void IsEnforcementEnabled_false_by_default_in_non_beta_builds()
        {
            Assert.False(BetaExpiryGate.IsEnforcementEnabled);
        }
#endif

#if BETA
        [Fact]
        public void IsEnforcementEnabled_true_in_beta_builds()
        {
            Assert.True(BetaExpiryGate.IsEnforcementEnabled);
        }
#endif
    }
}
