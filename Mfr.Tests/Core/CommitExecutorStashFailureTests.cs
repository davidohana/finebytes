using Mfr.Core;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Covers <see cref="CommitExecutor"/> behavior when <see cref="RenameItemMover.StashSourceToTemp"/> fails.
    /// </summary>
    public sealed class CommitExecutorStashFailureTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            RenameItemMover.StashSourceToTempSubstitute = null;
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies a failing stash records <see cref="RenameStatus.CommitError"/> and skips finalize for that row.
        /// </summary>
        public void Execute_StashFailure_RecordsCommitError_AndSkipsFinalizeForSameItem()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("a.txt");
            File.WriteAllText(sourcePath, "x");

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: dir,
                prefix: "a",
                extension: ".txt");
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            item.Preview.Prefix = "b";

            var tempPath = RenameItemMover.AllocateTempPath(item.Original.FullPath);
            var plan = new CommitPlan(
                Steps: [new StashStep(item, tempPath), new FinalizeStep(item, tempPath)],
                UnresolvableCycleItems: []);

            var simulateMessage = "mfr-test stash failure";
            RenameItemMover.StashSourceToTempSubstitute = (_, _) =>
                throw new IOException(simulateMessage);
            try
            {
                var results = CommitExecutor.Execute(
                    plan: plan,
                    allItems: [item],
                    confirmBeforeApply: null,
                    failFast: false,
                    dryRun: false);

                Assert.Single(results);
                Assert.Equal(RenameStatus.CommitError, results[0].Status);
                Assert.Equal(simulateMessage, results[0].Error);
                Assert.NotNull(item.CommitError);
                Assert.Equal(simulateMessage, item.CommitError.Message);
                Assert.True(File.Exists(sourcePath));
                Assert.False(File.Exists(tempPath));
                Assert.False(File.Exists(item.Preview.FullPath));
            }
            finally
            {
                RenameItemMover.StashSourceToTempSubstitute = null;
            }
        }

        [Fact]
        /// <summary>
        /// Verifies dry-run does not invoke stash (substitute stays unused).
        /// </summary>
        public void Execute_StashSubstitute_NotUsed_WhenDryRun()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("a.txt");
            File.WriteAllText(sourcePath, "x");

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: dir,
                prefix: "a",
                extension: ".txt");
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            item.Preview.Prefix = "b";

            var tempPath = RenameItemMover.AllocateTempPath(item.Original.FullPath);
            var plan = new CommitPlan(
                Steps: [new StashStep(item, tempPath), new FinalizeStep(item, tempPath)],
                UnresolvableCycleItems: []);

            var substituteCalled = false;
            RenameItemMover.StashSourceToTempSubstitute = (_, _) =>
            {
                substituteCalled = true;
                throw new IOException("should not run");
            };
            try
            {
                var results = CommitExecutor.Execute(
                    plan: plan,
                    allItems: [item],
                    confirmBeforeApply: null,
                    failFast: false,
                    dryRun: true);

                Assert.False(substituteCalled);
                Assert.Single(results);
                Assert.Equal(RenameStatus.CommitOk, results[0].Status);
            }
            finally
            {
                RenameItemMover.StashSourceToTempSubstitute = null;
            }
        }
    }
}
