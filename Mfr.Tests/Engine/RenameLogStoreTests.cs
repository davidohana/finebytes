using System.Text.Json;
using Mfr.Filters.Formatting;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests rename-commit log capture, retention trim, and limit-0 disk skip.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameLogStoreTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        public RenameLogStoreTests()
        {
            ConfigStoreTestReset.LoadEmpty();
            RenameLogStore.ClearLastOperation();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameLogStore.ClearLastOperation();
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies RenameList.Commit captures last op with destination path and changes (UI/CLI path).
        /// </summary>
        [Fact]
        public void RenameList_Commit_Captures_LastOperation_With_Destination_And_Changes()
        {
            ConfigStore.RenameLog.Limit = 0;
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var preset = _CreatePreset(
                "commit-captures",
                new FormatterFilter(Target: new FilePrefixTarget(), Options: new FormatterOptions("new-name"))
            );
            var plan = renameList.Preview(preset.Chain);
            var results = renameList.Commit(plan, failFast: false, dryRun: false);

            Assert.Equal(RenameStatus.CommitOk, Assert.Single(results).Status);
            Assert.Equal(dir.CombinePath("new-name.txt"), results[0].DestinationPath);
            Assert.Contains(
                results[0].Changes,
                c => c.Property == "Prefix" && c.OldValue == "old-name" && c.NewValue == "new-name"
            );

            Assert.NotNull(RenameLogStore.LastOperation);
            var entry = Assert.Single(RenameLogStore.LastOperation.Entries);
            Assert.Equal(dir.CombinePath("new-name.txt"), entry.DestinationPath);
            Assert.Equal(sourcePath, entry.OriginalPath);
            Assert.False(entry.IsFolder);
            Assert.Contains(
                entry.Changes,
                c => c.Property == "Prefix" && c.OldValue == "old-name" && c.NewValue == "new-name"
            );
        }

        /// <summary>
        /// Verifies CaptureFromCommit writes camelCase JSON <c>.mfrlog</c> when retention &gt; 0.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_Writes_Disk_Json()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("old.txt"),
                    destinationPath: TestPaths.Absolute("new.txt"),
                    oldPrefix: "old",
                    newPrefix: "new"
                ),
            };

            var writtenPath = RenameLogStore.CaptureFromCommit(results, directoryPath: logDir, limit: 10);

            Assert.NotNull(writtenPath);
            Assert.True(File.Exists(writtenPath));
            Assert.EndsWith(RenameLogStore.FileExtension, writtenPath, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(RenameLogStore.LastOperation);

            using var doc = JsonDocument.Parse(File.ReadAllText(writtenPath));
            Assert.True(doc.RootElement.TryGetProperty("committedAt", out _));
            Assert.Equal(
                TestPaths.Absolute("new.txt"),
                doc.RootElement.GetProperty("entries")[0].GetProperty("destinationPath").GetString()
            );
        }

        /// <summary>
        /// Verifies dry-run does not update last op or write disk.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_DryRun_Skips_Memory_And_Disk()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("old.txt"),
                    destinationPath: TestPaths.Absolute("new.txt"),
                    oldPrefix: "old",
                    newPrefix: "new"
                ),
            };

            var written = RenameLogStore.CaptureFromCommit(results, dryRun: true, directoryPath: logDir, limit: 10);

            Assert.Null(written);
            Assert.Null(RenameLogStore.LastOperation);
            Assert.Empty(Directory.EnumerateFiles(logDir));
        }

        /// <summary>
        /// Verifies limit 0 keeps memory last op but writes no disk file.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_Limit0_MemoryOnly_NoDisk()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("old.txt"),
                    destinationPath: TestPaths.Absolute("new.txt"),
                    oldPrefix: "old",
                    newPrefix: "new"
                ),
            };

            var written = RenameLogStore.CaptureFromCommit(results, directoryPath: logDir, limit: 0);

            Assert.Null(written);
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.Empty(Directory.EnumerateFiles(logDir));
        }

        /// <summary>
        /// Verifies a commit with no CommitOk rows leaves the previous last op unchanged.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_NoCommitOk_Leaves_LastOperation_Unchanged()
        {
            var prior = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("prior-old.txt"),
                    destinationPath: TestPaths.Absolute("prior-new.txt"),
                    oldPrefix: "prior-old",
                    newPrefix: "prior-new"
                ),
            };
            Assert.Null(RenameLogStore.CaptureFromCommit(prior, limit: 0));
            Assert.NotNull(RenameLogStore.LastOperation);
            var priorDestination = Assert.Single(RenameLogStore.LastOperation.Entries).DestinationPath;

            var skippedOnly = new[]
            {
                new RenameResultItem(
                    OriginalPath: TestPaths.Absolute("skip.txt"),
                    Status: RenameStatus.CommitSkipped,
                    Error: null,
                    Changes: [],
                    DestinationPath: null
                ),
            };
            Assert.Null(RenameLogStore.CaptureFromCommit(skippedOnly, limit: 0));
            Assert.Equal(priorDestination, Assert.Single(RenameLogStore.LastOperation.Entries).DestinationPath);
        }

        /// <summary>
        /// Verifies non-CommitOk rows are omitted from the log.
        /// </summary>
        [Fact]
        public void TryBuildFromCommitResults_Skips_NonCommitOk()
        {
            var results = new[]
            {
                new RenameResultItem(
                    OriginalPath: TestPaths.Absolute("skip.txt"),
                    Status: RenameStatus.CommitSkipped,
                    Error: null,
                    Changes: [],
                    DestinationPath: null
                ),
                new RenameResultItem(
                    OriginalPath: TestPaths.Absolute("err.txt"),
                    Status: RenameStatus.CommitError,
                    Error: "boom",
                    Changes: [],
                    DestinationPath: TestPaths.Absolute("err-dest.txt")
                ),
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("ok-old.txt"),
                    destinationPath: TestPaths.Absolute("ok-new.txt"),
                    oldPrefix: "ok-old",
                    newPrefix: "ok-new",
                    isFolder: true
                ),
            };

            var log = RenameLogStore.TryBuildFromCommitResults(results);
            Assert.NotNull(log);
            var entry = Assert.Single(log.Entries);
            Assert.Equal(TestPaths.Absolute("ok-new.txt"), entry.DestinationPath);
            Assert.True(entry.IsFolder);
        }

        /// <summary>
        /// Verifies retention pruning keeps only the newest N <c>.mfrlog</c> files.
        /// </summary>
        [Fact]
        public void PruneFiles_Keeps_Newest_Max()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 5; i++)
            {
                var logFilePath = logDirectoryPath.CombinePath($"{i:D3}{RenameLogStore.FileExtension}");
                File.WriteAllText(logFilePath, $"log-{i:D3}");
                File.SetCreationTimeUtc(logFilePath, baseTime.AddMinutes(i));
            }

            RenameLogStore.PruneFiles(logDirectoryPath, maxFiles: 2);

            var remainingNames = Directory
                .EnumerateFiles(logDirectoryPath, $"*{RenameLogStore.FileExtension}", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(2, remainingNames.Count);
            Assert.Contains($"003{RenameLogStore.FileExtension}", remainingNames);
            Assert.Contains($"004{RenameLogStore.FileExtension}", remainingNames);
        }

        /// <summary>
        /// Verifies unlimited retention does not delete files.
        /// </summary>
        [Fact]
        public void PruneFiles_Unlimited_Does_Not_Delete()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            for (var i = 0; i < 3; i++)
            {
                File.WriteAllText(logDirectoryPath.CombinePath($"{i}{RenameLogStore.FileExtension}"), "x");
            }

            RenameLogStore.PruneFiles(logDirectoryPath, maxFiles: int.MaxValue);

            Assert.Equal(3, Directory.EnumerateFiles(logDirectoryPath, $"*{RenameLogStore.FileExtension}").Count());
        }

        /// <summary>
        /// Verifies CaptureFromCommit trims after write when limit is finite.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_Trims_To_Limit()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 3; i++)
            {
                var existing = logDir.CombinePath($"old{i}{RenameLogStore.FileExtension}");
                File.WriteAllText(existing, "old");
                File.SetCreationTimeUtc(existing, baseTime.AddMinutes(i));
            }

            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("a.txt"),
                    destinationPath: TestPaths.Absolute("b.txt"),
                    oldPrefix: "a",
                    newPrefix: "b"
                ),
            };

            var written = RenameLogStore.CaptureFromCommit(results, directoryPath: logDir, limit: 2);
            Assert.NotNull(written);

            var remaining = Directory
                .EnumerateFiles(logDir, $"*{RenameLogStore.FileExtension}", SearchOption.TopDirectoryOnly)
                .ToList();
            Assert.Equal(2, remaining.Count);
            Assert.Contains(written, remaining);
        }

        private static RenameResultItem _CommitOkResult(
            string originalPath,
            string destinationPath,
            string oldPrefix,
            string newPrefix,
            bool isFolder = false
        )
        {
            return new RenameResultItem(
                OriginalPath: originalPath,
                Status: RenameStatus.CommitOk,
                Error: null,
                Changes: [new RenamePropertyChange("Prefix", oldPrefix, newPrefix)],
                DestinationPath: destinationPath,
                IsFolder: isFolder
            );
        }

        private static FilterPreset _CreatePreset(string name, BaseFilter filter)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = FilterChain.CreateAllEnabled([filter]),
            };
        }
    }
}
