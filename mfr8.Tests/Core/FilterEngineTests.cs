using Mfr8.Core;

namespace mfr8.Tests.Core
{
    /// <summary>
    /// Tests rename preview and commit behavior in the filter engine.
    /// </summary>
    public class FilterEngineTests
    {
        [Fact]
        /// <summary>
        /// Verifies that duplicate destinations are treated as conflicts and skipped.
        /// </summary>
        public void ConflictSkipped_ForDuplicateDestinations()
        {
            var dir = _CreateTempDir();

            try
            {
                var a = Path.Combine(dir, "a.mp3");
                var b = Path.Combine(dir, "b.mp3");
                File.WriteAllText(a, "x");
                File.WriteAllText(b, "y");

                var files = new List<FileEntryLite>
                {
                    new(GlobalIndex: 0, FolderOccurrenceIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "a", Extension: ".mp3"),
                    new(GlobalIndex: 1, FolderOccurrenceIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "b", Extension: ".mp3")
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

                var result = FilterEngine.PreviewAndCommit(preset, files, continueOnErrors: false);

                Assert.Equal(2, result.Conflicts);
                Assert.Equal(RenameStatus.ConflictSkipped, result.Results[0].Status);
                Assert.Equal(RenameStatus.ConflictSkipped, result.Results[1].Status);
                Assert.True(File.Exists(a), "source file 'a' should remain on conflict skip");
                Assert.True(File.Exists(b), "source file 'b' should remain on conflict skip");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        /// <summary>
        /// Verifies that the counter filter generates sequential names and commits expected moves.
        /// </summary>
        public void Renames_WithCounter()
        {
            var dir = _CreateTempDir();

            try
            {
                var a = Path.Combine(dir, "track01.mp3");
                var b = Path.Combine(dir, "track02.mp3");
                File.WriteAllText(a, "x");
                File.WriteAllText(b, "y");

                var files = new List<FileEntryLite>
                {
                    new(GlobalIndex: 0, FolderOccurrenceIndex: 0, FullPath: a, DirectoryPath: dir, Prefix: "track01", Extension: ".mp3"),
                    new(GlobalIndex: 1, FolderOccurrenceIndex: 0, FullPath: b, DirectoryPath: dir, Prefix: "track02", Extension: ".mp3"),
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

                var result = FilterEngine.PreviewAndCommit(preset, files, continueOnErrors: false);

                Assert.Equal(2, result.Renamed);
                Assert.Equal(RenameStatus.Ok, result.Results[0].Status);
                Assert.Equal(RenameStatus.Ok, result.Results[1].Status);

                Assert.False(File.Exists(a));
                Assert.False(File.Exists(b));
                Assert.True(File.Exists(Path.Combine(dir, "001.mp3")));
                Assert.True(File.Exists(Path.Combine(dir, "002.mp3")));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        private static string _CreateTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr8_tests_" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
