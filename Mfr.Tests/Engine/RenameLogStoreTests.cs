using System.Text.Json;
using Mfr.App.Ui.Services.RenameLog;
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
            Assert.False(RenameLogStore.LastOperation.IsUndo);
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
            Assert.Equal(writtenPath, RenameLogStore.LastWrittenFilePath);

            using var doc = JsonDocument.Parse(File.ReadAllText(writtenPath));
            Assert.True(doc.RootElement.TryGetProperty("committedAt", out _));
            Assert.False(doc.RootElement.GetProperty("isUndo").GetBoolean());
            Assert.Equal(
                TestPaths.Absolute("new.txt"),
                doc.RootElement.GetProperty("entries")[0].GetProperty("destinationPath").GetString()
            );
        }

        /// <summary>
        /// Verifies CaptureFromCommit with <c>isUndo</c> persists the flag and details label Undo.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_IsUndo_Persists_And_Shows_In_Details()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("new.txt"),
                    destinationPath: TestPaths.Absolute("old.txt"),
                    oldPrefix: "new",
                    newPrefix: "old"
                ),
            };

            var writtenPath = RenameLogStore.CaptureFromCommit(results, directoryPath: logDir, limit: 10, isUndo: true);

            Assert.NotNull(writtenPath);
            Assert.True(RenameLogStore.LastOperation!.IsUndo);
            Assert.Contains(
                "Operation: Undo",
                RenameLogDisplay.FormatDetails(RenameLogStore.LastOperation),
                StringComparison.Ordinal
            );
            Assert.Equal("Undo · 1 item", RenameLogDisplay.FormatListSummary(RenameLogStore.LastOperation));

            using var doc = JsonDocument.Parse(File.ReadAllText(writtenPath));
            Assert.True(doc.RootElement.GetProperty("isUndo").GetBoolean());

            var loaded = RenameLogStore.TryLoadFile(writtenPath);
            Assert.NotNull(loaded);
            Assert.True(loaded.IsUndo);
            Assert.Contains("Operation: Undo", RenameLogDisplay.FormatDetails(loaded), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies JSON without <c>isUndo</c> soft-loads as GO.
        /// </summary>
        [Fact]
        public void TryLoadFile_Missing_IsUndo_Defaults_To_Go()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var path = logDir.CombinePath($"20260101120000{RenameLogStore.FileExtension}");
            var destinationPath = TestPaths.Absolute("a.txt");
            var originalPath = TestPaths.Absolute("b.txt");
            var destinationJson = JsonSerializer.Serialize(destinationPath);
            var originalJson = JsonSerializer.Serialize(originalPath);
            File.WriteAllText(
                path,
                $$"""
                {
                  "committedAt": "2026-01-01T12:00:00+00:00",
                  "entries": [
                    {
                      "destinationPath": {{destinationJson}},
                      "originalPath": {{originalJson}},
                      "isFolder": false,
                      "changes": [ { "property": "Prefix", "oldValue": "b", "newValue": "a" } ]
                    }
                  ]
                }
                """
            );

            var loaded = RenameLogStore.TryLoadFile(path);

            Assert.NotNull(loaded);
            Assert.False(loaded.IsUndo);
            Assert.Contains("Operation: GO", RenameLogDisplay.FormatDetails(loaded), StringComparison.Ordinal);
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
        /// Verifies a commit with no loggable rows leaves the previous last op unchanged.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_NoLoggableRows_Leaves_LastOperation_Unchanged()
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
        /// Verifies skipped / preview-error rows are omitted and CommitError rows are included.
        /// </summary>
        [Fact]
        public void TryBuildFromCommitResults_Includes_CommitError_Skips_NonApplied()
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
                    OriginalPath: TestPaths.Absolute("preview.txt"),
                    Status: RenameStatus.PreviewError,
                    Error: "preview fail",
                    Changes: [],
                    DestinationPath: null
                ),
                _CommitErrorResult(
                    originalPath: TestPaths.Absolute("err.txt"),
                    error: "boom",
                    changes: [new RenamePropertyChange("Prefix", "a", "b")],
                    destinationPath: TestPaths.Absolute("err-dest.txt")
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
            Assert.Equal(2, log.Entries.Count);

            var errorEntry = log.Entries[0];
            Assert.Equal(TestPaths.Absolute("err-dest.txt"), errorEntry.DestinationPath);
            Assert.Equal(TestPaths.Absolute("err.txt"), errorEntry.OriginalPath);
            Assert.Equal("boom", errorEntry.Error);
            Assert.False(errorEntry.IsUndoable);

            var okEntry = log.Entries[1];
            Assert.Equal(TestPaths.Absolute("ok-new.txt"), okEntry.DestinationPath);
            Assert.True(okEntry.IsFolder);
            Assert.True(okEntry.IsUndoable);
            Assert.True(log.HasUndoableEntries);
        }

        /// <summary>
        /// Verifies mixed CommitOk + CommitError capture sets LastOperation and keeps both rows.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_Mixed_Includes_Error_And_Sets_LastOperation()
        {
            var results = new[]
            {
                _CommitOkResult(
                    originalPath: TestPaths.Absolute("ok-old.txt"),
                    destinationPath: TestPaths.Absolute("ok-new.txt"),
                    oldPrefix: "ok-old",
                    newPrefix: "ok-new"
                ),
                _CommitErrorResult(originalPath: TestPaths.Absolute("err.txt"), error: "disk full"),
            };

            Assert.Null(RenameLogStore.CaptureFromCommit(results, limit: 0));
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.Equal(2, RenameLogStore.LastOperation.Entries.Count);
            Assert.True(RenameLogStore.LastOperation.HasUndoableEntries);

            var errorEntry = RenameLogStore.LastOperation.Entries[1];
            Assert.Equal("disk full", errorEntry.Error);
            Assert.Equal(TestPaths.Absolute("err.txt"), errorEntry.DestinationPath);
            Assert.False(errorEntry.IsUndoable);
        }

        /// <summary>
        /// Verifies errors-only capture leaves prior LastOperation and still writes disk when retention &gt; 0.
        /// </summary>
        [Fact]
        public void CaptureFromCommit_ErrorsOnly_Leaves_LastOperation_Writes_Disk()
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

            var logDir = _tempDirectoryFixture.CreateTempDir();
            var errorsOnly = new[]
            {
                _CommitErrorResult(originalPath: TestPaths.Absolute("err.txt"), error: "access denied"),
            };

            var writtenPath = RenameLogStore.CaptureFromCommit(errorsOnly, directoryPath: logDir, limit: 10);
            Assert.NotNull(writtenPath);
            Assert.True(File.Exists(writtenPath));
            Assert.Equal(priorDestination, Assert.Single(RenameLogStore.LastOperation.Entries).DestinationPath);

            var loaded = RenameLogStore.TryLoadFile(writtenPath);
            Assert.NotNull(loaded);
            Assert.False(loaded.HasUndoableEntries);
            var errorEntry = Assert.Single(loaded.Entries);
            Assert.Equal("access denied", errorEntry.Error);
            Assert.Equal(TestPaths.Absolute("err.txt"), errorEntry.OriginalPath);
        }

        /// <summary>
        /// Verifies FormatDetails uses OriginalPath for error rows and DestinationPath for OK rows.
        /// </summary>
        [Fact]
        public void FormatDetails_Error_Uses_OriginalPath_As_Item()
        {
            var log = new RenameLog(
                CommittedAt: DateTimeOffset.Parse("2026-01-15T12:00:00Z"),
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: TestPaths.Absolute("ok-new.txt"),
                        OriginalPath: TestPaths.Absolute("ok-old.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "old", "new")]
                    ),
                    new RenameLogEntry(
                        DestinationPath: TestPaths.Absolute("err-dest.txt"),
                        OriginalPath: TestPaths.Absolute("err-src.txt"),
                        IsFolder: false,
                        Changes: [],
                        Error: "not found"
                    ),
                ]
            );

            Assert.Equal(TestPaths.Absolute("ok-new.txt"), log.Entries[0].DetailsItemPath);
            Assert.Equal(TestPaths.Absolute("err-src.txt"), log.Entries[1].DetailsItemPath);

            var details = RenameLogDisplay.FormatDetails(log);
            Assert.Contains("Item: " + TestPaths.Absolute("ok-new.txt"), details, StringComparison.Ordinal);
            Assert.Contains("Item: " + TestPaths.Absolute("err-src.txt"), details, StringComparison.Ordinal);
            Assert.Contains("Error: not found", details, StringComparison.Ordinal);
            Assert.DoesNotContain("Item: " + TestPaths.Absolute("err-dest.txt"), details, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies FormatDetails truncates after <see cref="RenameLogDisplay.MaxDetailsEntries"/> and notes the remainder.
        /// </summary>
        [Fact]
        public void FormatDetails_Truncates_Large_Logs()
        {
            var entries = Enumerable
                .Range(0, RenameLogDisplay.MaxDetailsEntries + 25)
                .Select(i => new RenameLogEntry(
                    DestinationPath: TestPaths.Absolute($"new-{i}.txt"),
                    OriginalPath: TestPaths.Absolute($"old-{i}.txt"),
                    IsFolder: false,
                    Changes: [new RenamePropertyChange("Prefix", $"old-{i}", $"new-{i}")]
                ))
                .ToList();
            var log = new RenameLog(CommittedAt: DateTimeOffset.Parse("2026-01-15T12:00:00Z"), Entries: entries);

            var details = RenameLogDisplay.FormatDetails(log);

            Assert.Contains($"Processed {entries.Count} Items", details, StringComparison.Ordinal);
            Assert.Contains("Item: " + TestPaths.Absolute("new-0.txt"), details, StringComparison.Ordinal);
            Assert.Contains(
                "Item: " + TestPaths.Absolute($"new-{RenameLogDisplay.MaxDetailsEntries - 1}.txt"),
                details,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Item: " + TestPaths.Absolute($"new-{RenameLogDisplay.MaxDetailsEntries}.txt"),
                details,
                StringComparison.Ordinal
            );
            Assert.Contains("… and 25 more item(s).", details, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies list summary shows GO/Undo and singular/plural item counts.
        /// </summary>
        [Fact]
        public void FormatListSummary_Shows_Operation_And_Count()
        {
            var goOne = new RenameLog(
                CommittedAt: DateTimeOffset.Parse("2026-01-15T12:00:00Z"),
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: TestPaths.Absolute("a.txt"),
                        OriginalPath: TestPaths.Absolute("b.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "b", "a")]
                    ),
                ]
            );
            var undoMany = new RenameLog(
                CommittedAt: DateTimeOffset.Parse("2026-01-15T13:00:00Z"),
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: TestPaths.Absolute("1.txt"),
                        OriginalPath: TestPaths.Absolute("1a.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "1a", "1")]
                    ),
                    new RenameLogEntry(
                        DestinationPath: TestPaths.Absolute("2.txt"),
                        OriginalPath: TestPaths.Absolute("2a.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "2a", "2")]
                    ),
                ],
                IsUndo: true
            );

            Assert.Equal("GO · 1 item", RenameLogDisplay.FormatListSummary(goOne));
            Assert.Equal("Undo · 2 items", RenameLogDisplay.FormatListSummary(undoMany));
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
        /// Verifies limit 0 deletes all on-disk logs (Options Disabled / MFR7 LogLimit 0).
        /// </summary>
        [Fact]
        public void PruneFiles_Zero_Deletes_All()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            for (var i = 0; i < 3; i++)
            {
                File.WriteAllText(logDirectoryPath.CombinePath($"{i}{RenameLogStore.FileExtension}"), "x");
            }

            RenameLogStore.PruneFiles(logDirectoryPath, maxFiles: 0);

            Assert.Empty(Directory.EnumerateFiles(logDirectoryPath, $"*{RenameLogStore.FileExtension}"));
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

        /// <summary>
        /// Verifies disk list is newest-first by filename and titles format from yyyyMMddHHmmss stamps.
        /// </summary>
        [Fact]
        public void ListDiskFilePaths_Newest_First_And_FormatDiskListTitle()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var older = logDir.CombinePath($"20260101000000{RenameLogStore.FileExtension}");
            var newer = logDir.CombinePath($"20260201120030{RenameLogStore.FileExtension}");
            File.WriteAllText(older, "{}");
            File.WriteAllText(newer, "{}");

            var listed = RenameLogStore.ListDiskFilePaths(logDir);

            Assert.Equal([newer, older], listed);
            Assert.Equal("01/02/2026 12:00:30", RenameLogDisplay.FormatDiskListTitle(newer));
            Assert.Equal(
                "01/01/2026 00:00:00",
                RenameLogDisplay.FormatDiskListTitle(
                    logDir.CombinePath($"20260101000000-1{RenameLogStore.FileExtension}")
                )
            );
        }

        /// <summary>
        /// Verifies TryLoadFile rejects JSON without an entries array (avoids null Entries NREs in the UI).
        /// </summary>
        [Fact]
        public void TryLoadFile_Rejects_Missing_Entries()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var path = logDir.CombinePath($"20260101000000{RenameLogStore.FileExtension}");
            File.WriteAllText(path, "{}");

            Assert.Null(RenameLogStore.TryLoadFile(path));
        }

        /// <summary>
        /// Verifies TryLoadFile round-trips CaptureFromCommit JSON and TryDeleteFile removes it.
        /// </summary>
        [Fact]
        public void TryLoadFile_And_TryDeleteFile_RoundTrip()
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

            var loaded = RenameLogStore.TryLoadFile(writtenPath);
            Assert.NotNull(loaded);
            Assert.True(loaded.HasUndoableEntries);
            Assert.False(loaded.IsUndo);
            Assert.Equal(TestPaths.Absolute("new.txt"), Assert.Single(loaded.Entries).DestinationPath);
            Assert.Contains("Operation: GO", RenameLogDisplay.FormatDetails(loaded), StringComparison.Ordinal);
            Assert.Contains("Changed 'Prefix'", RenameLogDisplay.FormatDetails(loaded));

            Assert.True(RenameLogStore.TryDeleteFile(writtenPath));
            Assert.False(File.Exists(writtenPath));
            Assert.Null(RenameLogStore.TryLoadFile(writtenPath));
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

        private static RenameResultItem _CommitErrorResult(
            string originalPath,
            string error,
            IReadOnlyList<RenamePropertyChange>? changes = null,
            string? destinationPath = null
        )
        {
            return new RenameResultItem(
                OriginalPath: originalPath,
                Status: RenameStatus.CommitError,
                Error: error,
                Changes: changes ?? [],
                DestinationPath: destinationPath
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
