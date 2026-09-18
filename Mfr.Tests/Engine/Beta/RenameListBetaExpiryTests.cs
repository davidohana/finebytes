using Mfr.Engine.Beta;
using Mfr.Filters.Formatting;
using Mfr.Utils;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Engine.Beta
{
    /// <summary>
    /// Commit-path tests for beta expiry enforcement on <see cref="RenameList.Commit"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="BetaExpiryGate.SetEnforceForTests"/> so non-BETA local test runs can assert
    /// the throw/dry-run behavior without <c>-p:BETA=true</c>.
    /// </para>
    /// </remarks>
    [Collection(BetaExpiryGateCollection.Name)]
    public sealed class RenameListBetaExpiryTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temp files and restores beta gate defaults.
        /// </summary>
        public void Dispose()
        {
            BetaExpiryGate.ResetForTests();
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        public void Commit_throws_when_expired_and_not_dry_run()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(true);

            var (renameList, plan, sourcePath) = _PrepareSimpleRename();

            var ex = Assert.Throws<BetaExpiredException>(() => renameList.Commit(plan, failFast: false, dryRun: false));
            Assert.Equal(BetaExpiryGate.ExpiresUtc, ex.ExpiresUtc);
            Assert.True(File.Exists(sourcePath));
        }

        [Fact]
        public void Commit_allows_dry_run_when_expired()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(true);

            var (renameList, plan, sourcePath) = _PrepareSimpleRename();
            var destPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, "renamed.txt");

            var results = renameList.Commit(plan, failFast: false, dryRun: true);

            Assert.Equal(RenameStatus.CommitOk, Assert.Single(results).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(destPath));
        }

#if !BETA
        [Fact]
        public void Commit_does_not_throw_when_expired_if_enforcement_off()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );
            BetaExpiryGate.SetEnforceForTests(false);

            var (renameList, plan, sourcePath) = _PrepareSimpleRename();
            var destPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, "renamed.txt");

            var results = renameList.Commit(plan, failFast: false, dryRun: false);

            Assert.Equal(RenameStatus.CommitOk, Assert.Single(results).Status);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destPath));
        }
#endif

        private (RenameList RenameList, CommitPlan Plan, string SourcePath) _PrepareSimpleRename()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("original.txt");
            File.WriteAllText(sourcePath, "x");
            var destPath = dir.CombinePath("renamed.txt");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "beta-expiry-rename",
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new FormatterFilter(Target: new FullPathTarget(), Options: new FormatterOptions(destPath)),
                ]),
            };
            var plan = renameList.Preview(preset.Chain);
            return (renameList, plan, sourcePath);
        }
    }
}
