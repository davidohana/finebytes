using Mfr.Core;
using Mfr.Models;
using Mfr.Models.Filters.Advanced;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests rename preview and commit behavior in the filter engine.
    /// </summary>
    public class FilterEngineTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies that duplicate destinations are treated as conflicts and skipped.
        /// </summary>
        public void ConflictSkipped_ForDuplicateDestinations()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var a = dir.CombinePath("a.mp3");
            var b = dir.CombinePath("b.mp3");
            File.WriteAllText(a, "x");
            File.WriteAllText(b, "y");

            var files = new List<RenameItem>
            {
                new(new FileEntryLite(GlobalIndex: 0, InFolderIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "a", Extension: ".mp3")),
                new(new FileEntryLite(GlobalIndex: 1, InFolderIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "b", Extension: ".mp3"))
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "duplicate",
                Description = null,
                Filters =
                [
                    new FormatterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Full),
                        Options: new FormatterOptions("same.mp3"))
                ]
            };

            var preview = FilterEngine.Preview(preset, files, failFast: false);
            var result = FilterEngine.Commit(preset.Name, files, failFast: false);

            Assert.Equal(2, result.Conflicts);
            Assert.Equal(RenameStatus.ConflictSkipped, result.Results[0].Status);
            Assert.Equal(RenameStatus.ConflictSkipped, result.Results[1].Status);
            Assert.Equal(0, preview.Errors);
            Assert.True(File.Exists(a), "source file 'a' should remain on conflict skip");
            Assert.True(File.Exists(b), "source file 'b' should remain on conflict skip");
        }

        [Fact]
        /// <summary>
        /// Verifies that the counter filter generates sequential names and commits expected moves.
        /// </summary>
        public void Renames_WithCounter()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var a = dir.CombinePath("track01.mp3");
            var b = dir.CombinePath("track02.mp3");
            File.WriteAllText(a, "x");
            File.WriteAllText(b, "y");

            var files = new List<RenameItem>
            {
                new(new FileEntryLite(GlobalIndex: 0, InFolderIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "track01", Extension: ".mp3")),
                new(new FileEntryLite(GlobalIndex: 1, InFolderIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "track02", Extension: ".mp3")),
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ]
            };

            var preview = FilterEngine.Preview(preset, files, failFast: false);
            var result = FilterEngine.Commit(preset.Name, files, failFast: false);

            Assert.Equal(2, result.Renamed);
            Assert.Equal(RenameStatus.Ok, result.Results[0].Status);
            Assert.Equal(RenameStatus.Ok, result.Results[1].Status);
            Assert.Equal(0, preview.Errors);

            Assert.False(File.Exists(a));
            Assert.False(File.Exists(b));
            Assert.True(File.Exists(dir.CombinePath("001.mp3")));
            Assert.True(File.Exists(dir.CombinePath("002.mp3")));
        }

        [Fact]
        /// <summary>
        /// Verifies that commit stops immediately on the first rename error when fail-fast is enabled.
        /// </summary>
        public void Commit_StopsOnFirstRenameError_WhenFailFastTrue()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSource = dir.CombinePath("first.mp3");
            var secondSource = dir.CombinePath("second.mp3");
            File.WriteAllText(firstSource, "x");
            File.WriteAllText(secondSource, "y");

            var files = new List<RenameItem>
            {
                new(new FileEntryLite(GlobalIndex: 0, InFolderIndex: 0, FullPath: firstSource, DirectoryPath: dir, Prefix: "first", Extension: ".mp3")),
                new(new FileEntryLite(GlobalIndex: 1, InFolderIndex: 0, FullPath: secondSource, DirectoryPath: dir, Prefix: "second", Extension: ".mp3")),
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ]
            };

            var preview = FilterEngine.Preview(preset, files, failFast: false);
            Assert.Equal(0, preview.Errors);

            File.Delete(firstSource);

            var result = FilterEngine.Commit(preset.Name, files, failFast: true);

            Assert.Equal(1, result.Errors);
            Assert.Equal(0, result.Renamed);
            Assert.Equal(RenameStatus.Error, result.Results[0].Status);
            Assert.Equal(RenameStatus.Skipped, result.Results[1].Status);
            Assert.True(File.Exists(secondSource));
            Assert.False(File.Exists(dir.CombinePath("002.mp3")));
        }

        [Fact]
        /// <summary>
        /// Verifies that commit continues after a rename error when fail-fast is disabled.
        /// </summary>
        public void Commit_ContinuesAfterRenameError_WhenFailFastFalse()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSource = dir.CombinePath("first.mp3");
            var secondSource = dir.CombinePath("second.mp3");
            File.WriteAllText(firstSource, "x");
            File.WriteAllText(secondSource, "y");

            var files = new List<RenameItem>
            {
                new(new FileEntryLite(GlobalIndex: 0, InFolderIndex: 0, FullPath: firstSource, DirectoryPath: dir, Prefix: "first", Extension: ".mp3")),
                new(new FileEntryLite(GlobalIndex: 1, InFolderIndex: 0, FullPath: secondSource, DirectoryPath: dir, Prefix: "second", Extension: ".mp3")),
            };

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNameTargetMode.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ]
            };

            var preview = FilterEngine.Preview(preset, files, failFast: false);
            Assert.Equal(0, preview.Errors);

            File.Delete(firstSource);

            var result = FilterEngine.Commit(preset.Name, files, failFast: false);

            Assert.Equal(1, result.Errors);
            Assert.Equal(1, result.Renamed);
            Assert.Equal(RenameStatus.Error, result.Results[0].Status);
            Assert.Equal(RenameStatus.Ok, result.Results[1].Status);
            Assert.True(File.Exists(dir.CombinePath("002.mp3")));
            Assert.False(File.Exists(secondSource));
        }
    }
}
