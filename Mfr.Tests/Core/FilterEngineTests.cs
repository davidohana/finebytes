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

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([a, b]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "duplicate",
                Description = null,
                Filters =
                [
                    new FormatterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNamePart.Full),
                        Options: new FormatterOptions("same.mp3"))
                ]
            };

            renameList.Preview(preset, failFast: false);
            var result = FilterEngine.Commit(files, failFast: false);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitConflictSkipped));
            Assert.Equal(RenameStatus.CommitConflictSkipped, result[0].Status);
            Assert.Equal(RenameStatus.CommitConflictSkipped, result[1].Status);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);
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

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([a, b]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNamePart.Prefix),
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

            renameList.Preview(preset, failFast: false);
            var result = FilterEngine.Commit(files, failFast: false);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);
            Assert.Equal(RenameStatus.CommitOk, result[1].Status);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

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

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([firstSource, secondSource]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNamePart.Prefix),
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

            renameList.Preview(preset, failFast: false);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

            File.Delete(firstSource);

            var result = FilterEngine.Commit(files, failFast: true);

            Assert.Equal(1, result.Count(x => x.Status == RenameStatus.CommitError));
            Assert.Equal(0, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.Equal(RenameStatus.CommitError, result[0].Status);
            Assert.Equal(RenameStatus.CommitSkipped, result[1].Status);
            var firstCommitError = files[0].CommitError;
            Assert.NotNull(firstCommitError);
            Assert.NotNull(firstCommitError.Cause);
            Assert.Equal(firstCommitError.Cause.Message, firstCommitError.Message);
            Assert.Null(files[1].CommitError);
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

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([firstSource, secondSource]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNamePart.Prefix),
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

            renameList.Preview(preset, failFast: false);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

            File.Delete(firstSource);

            var result = FilterEngine.Commit(files, failFast: false);

            Assert.Equal(1, result.Count(x => x.Status == RenameStatus.CommitError));
            Assert.Equal(1, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.Equal(RenameStatus.CommitError, result[0].Status);
            Assert.Equal(RenameStatus.CommitOk, result[1].Status);
            var firstCommitError = files[0].CommitError;
            Assert.NotNull(firstCommitError);
            Assert.NotNull(firstCommitError.Cause);
            Assert.Equal(firstCommitError.Cause.Message, firstCommitError.Message);
            Assert.Null(files[1].CommitError);
            Assert.True(File.Exists(dir.CombinePath("002.mp3")));
            Assert.False(File.Exists(secondSource));
        }

        [Fact]
        /// <summary>
        /// Verifies that preview errors are cleared before each new preview run.
        /// </summary>
        public void Preview_ResetsPreviewError_OnEachRun()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = dir.CombinePath("track.mp3");
            File.WriteAllText(source, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([source]);
            var files = renameList.RenameItems;

            var failingPreset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "failing-preview",
                Description = null,
                Filters =
                [
                    new ReplacerFilter(
                        Enabled: true,
                        Target: new UnsupportedTarget(),
                        Options: new ReplacerOptions("a", "b", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
                ]
            };

            renameList.Preview(failingPreset, failFast: false);
            var previewError = files[0].PreviewError;
            Assert.NotNull(previewError);
            Assert.NotNull(previewError.Cause);
            Assert.Equal(previewError.Cause.Message, previewError.Message);

            var successPreset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "successful-preview",
                Description = null,
                Filters = []
            };

            renameList.Preview(successPreset, failFast: false);
            Assert.Null(files[0].PreviewError);
        }

        private sealed record UnsupportedTarget : FilterTarget
        {
            public override FilterTargetFamily Family => FilterTargetFamily.FileContents;
        }
    }
}
