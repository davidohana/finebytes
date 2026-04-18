using Mfr.Core;
using Mfr.Filters.Attributes;
using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests rename preview and commit behavior through <see cref="RenameList"/>.
    /// </summary>
    public class RenameListCommitTests : IDisposable
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
        /// Verifies that duplicate destinations are surfaced as commit-time rename errors.
        /// </summary>
        public void CommitError_ForDuplicateDestinations()
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
                Chain = FilterChain.CreateAllEnabled(
                [
                    new FormatterFilter(
                        Target: new FileNameTarget(FileNamePart.Full),
                        Options: new FormatterOptions("same.mp3"))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false);

            Assert.Equal(1, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.Equal(1, result.Count(x => x.Status == RenameStatus.CommitError));
            Assert.Equal(2, files.Count(item => item.PreviewError is not null));
            Assert.Equal(1, files.Count(item => item.CommitError is not null));
            Assert.True(File.Exists(a) ^ File.Exists(b), "exactly one source file should remain after one succeeds and one fails");
            Assert.True(File.Exists(dir.CombinePath("same.mp3")));
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
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false);

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
        /// Verifies that dry-run commit reports success for changed items without moving files.
        /// </summary>
        public void Commit_DryRun_ReportsCommitOk_WithoutMovingFiles()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSource = dir.CombinePath("track01.mp3");
            var secondSource = dir.CombinePath("track02.mp3");
            File.WriteAllText(firstSource, "x");
            File.WriteAllText(secondSource, "y");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([firstSource, secondSource]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false, dryRun: true);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.True(File.Exists(firstSource));
            Assert.True(File.Exists(secondSource));
            Assert.False(File.Exists(dir.CombinePath("001.mp3")));
            Assert.False(File.Exists(dir.CombinePath("002.mp3")));
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>confirmBeforeApply</c> returning <c>false</c> skips renames without moving files.
        /// </summary>
        public void Commit_ConfirmBeforeApply_AllFalse_SkipsAllRenames()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSource = dir.CombinePath("track01.mp3");
            var secondSource = dir.CombinePath("track02.mp3");
            File.WriteAllText(firstSource, "x");
            File.WriteAllText(secondSource, "y");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([firstSource, secondSource]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false, dryRun: false, confirmBeforeApply: _ => false);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitSkipped));
            Assert.Equal(0, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.True(File.Exists(firstSource));
            Assert.True(File.Exists(secondSource));
            Assert.False(File.Exists(dir.CombinePath("001.mp3")));
            Assert.False(File.Exists(dir.CombinePath("002.mp3")));
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>confirmBeforeApply</c> returning <c>true</c> matches commit behavior without a callback.
        /// </summary>
        public void Commit_ConfirmBeforeApply_AllTrue_RenamesFiles()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSource = dir.CombinePath("track01.mp3");
            var secondSource = dir.CombinePath("track02.mp3");
            File.WriteAllText(firstSource, "x");
            File.WriteAllText(secondSource, "y");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([firstSource, secondSource]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false, dryRun: false, confirmBeforeApply: _ => true);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.False(File.Exists(firstSource));
            Assert.False(File.Exists(secondSource));
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
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

            File.Delete(firstSource);

            var result = renameList.Commit(failFast: true);

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
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

            File.Delete(firstSource);

            var result = renameList.Commit(failFast: false);

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
                Chain = FilterChain.CreateAllEnabled(
                [
                    new ReplacerFilter(
                        Target: new UnsupportedTarget(),
                        Options: new ReplacerOptions("a", "b", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
                ])
            };

            failingPreset.Chain.SetupFilters();
            renameList.Preview(failingPreset);
            var previewError = files[0].PreviewError;
            Assert.NotNull(previewError);
            Assert.NotNull(previewError.Cause);
            Assert.Equal(previewError.Cause.Message, previewError.Message);

            var successPreset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "successful-preview",
                Description = null,
                Chain = new FilterChain { Steps = [] }
            };

            successPreset.Chain.SetupFilters();
            renameList.Preview(successPreset);
            Assert.Null(files[0].PreviewError);
        }

        [Fact]
        /// <summary>
        /// Verifies that commit skips items when preview has not been run.
        /// </summary>
        public void Commit_SkipsItem_WhenPreviewIsMissing()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = dir.CombinePath("track.mp3");
            File.WriteAllText(source, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([source]);
            var files = renameList.RenameItems;

            var result = renameList.Commit(failFast: false);

            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitSkipped, result[0].Status);
            Assert.Equal(source, result[0].OriginalPath);
            Assert.Empty(result[0].Changes);
            Assert.Null(files[0].CommitError);
            Assert.True(File.Exists(source));
        }

        [Fact]
        /// <summary>
        /// Verifies that commit skips items whose preview destination equals the source path.
        /// </summary>
        public void Commit_SkipsItem_WhenPreviewDestinationMatchesSource()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = dir.CombinePath("track.mp3");
            File.WriteAllText(source, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([source]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "no-change",
                Description = null,
                Chain = new FilterChain { Steps = [] }
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false);

            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitSkipped, result[0].Status);
            Assert.Empty(result[0].Changes);
            Assert.Null(files[0].CommitError);
            Assert.True(File.Exists(source));
        }

        [Fact]
        /// <summary>
        /// Verifies that dry-run commit still skips items whose preview destination equals source.
        /// </summary>
        public void Commit_DryRun_SkipsItem_WhenPreviewDestinationMatchesSource()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = dir.CombinePath("track.mp3");
            File.WriteAllText(source, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([source]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "no-change",
                Description = null,
                Chain = new FilterChain { Steps = [] }
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false, dryRun: true);

            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitSkipped, result[0].Status);
            Assert.Empty(result[0].Changes);
            Assert.Null(files[0].CommitError);
            Assert.True(File.Exists(source));
        }

        [Fact]
        /// <summary>
        /// Verifies that commit reports an error when destination already exists on disk.
        /// </summary>
        public void Commit_ErrorsItem_WhenDestinationAlreadyExists()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = dir.CombinePath("track.mp3");
            var existingDestination = dir.CombinePath("001.mp3");
            File.WriteAllText(source, "x");
            File.WriteAllText(existingDestination, "occupied");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([source]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new CounterFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var result = renameList.Commit(failFast: false);

            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitError, result[0].Status);
            Assert.Empty(result[0].Changes);
            Assert.True(File.Exists(source));
            Assert.True(File.Exists(existingDestination));
        }

        [Fact]
        /// <summary>
        /// Verifies that an existing destination is allowed when that path is also being moved away in this batch.
        /// </summary>
        public void Commit_AllowsExistingDestination_WhenItWillBeRenamedAway()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourceA = dir.CombinePath("a.mp3");
            var sourceB = dir.CombinePath("b.mp3");
            var destinationC = dir.CombinePath("c.mp3");
            File.WriteAllText(sourceA, "x");
            File.WriteAllText(sourceB, "y");

            var renameList = new RenameList(includeHidden: true);
            // Order matters for one-pass commit: move b->c first, then a->b.
            renameList.AddSources([sourceB, sourceA]);
            var files = renameList.RenameItems;

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "chain-shift",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new ReplacerFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new ReplacerOptions("b", "c", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: false, WholeWord: false)),
                    new ReplacerFilter(
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new ReplacerOptions("a", "b", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: false, WholeWord: false))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            Assert.DoesNotContain(files, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);

            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.DoesNotContain(result, item => item.Status == RenameStatus.CommitError);
            Assert.False(File.Exists(sourceA));
            Assert.True(File.Exists(sourceB));
            Assert.True(File.Exists(destinationC));
        }

        [Fact]
        /// <summary>
        /// Verifies attribute-only preview commits call <see cref="File.SetAttributes"/> on the source path.
        /// </summary>
        public void Commit_AttributesOnly_AppliesHidden()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("x.txt");
            File.WriteAllText(path, "x");
            var attrsBefore = File.GetAttributes(path);
            Assert.False(attrsBefore.HasFlag(FileAttributes.Hidden));

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "attrs",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new AttributesSetterFilter(
                        Target: new AttributesTarget(),
                        Options: new AttributesSetterOptions(
                            ReadOnly: AttributeTriState.Keep,
                            Hidden: AttributeTriState.Set,
                            Archive: AttributeTriState.Keep,
                            System: AttributeTriState.Keep))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Hidden));

            var result = renameList.Commit(failFast: false);
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);
            Assert.Contains(result[0].Changes, c => c.Property == "Attributes");

            var attrsAfter = File.GetAttributes(path);
            Assert.True(attrsAfter.HasFlag(FileAttributes.Hidden));
        }

        [Fact]
        /// <summary>
        /// Verifies dry-run commit does not apply attribute changes on disk.
        /// </summary>
        public void Commit_AttributesOnly_DryRun_LeavesAttributes()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("y.txt");
            File.WriteAllText(path, "y");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "attrs-dry",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new AttributesSetterFilter(
                        Target: new AttributesTarget(),
                        Options: new AttributesSetterOptions(
                            ReadOnly: AttributeTriState.Keep,
                            Hidden: AttributeTriState.Set,
                            Archive: AttributeTriState.Keep,
                            System: AttributeTriState.Keep))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);

            var result = renameList.Commit(failFast: false, dryRun: true);
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);

            var attrsAfter = File.GetAttributes(path);
            Assert.False(attrsAfter.HasFlag(FileAttributes.Hidden));
        }

        [Fact]
        /// <summary>
        /// Verifies that last-write time preview commits call <see cref="File.SetLastWriteTime"/>.
        /// </summary>
        public void Commit_LastWriteTimeOnly_AppliesDateSetter()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("dated.txt");
            File.WriteAllText(path, "x");
            var before = File.GetLastWriteTime(path);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "last-write",
                Description = null,
                Chain = FilterChain.CreateAllEnabled(
                [
                    new DateSetterFilter(
                        Target: new LastWriteDateTarget(),
                        Options: new DateSetterOptions(Date: DateOnly.FromDateTime(before.AddDays(30))))
                ])
            };

            preset.Chain.SetupFilters();
            renameList.Preview(preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.NotEqual(before, item.Preview.LastWriteTime);

            var result = renameList.Commit(failFast: false);
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);
            Assert.Contains(result[0].Changes, c => c.Property == "LastWriteTime");

            var after = File.GetLastWriteTime(path);
            Assert.Equal(item.Preview.LastWriteTime, after);
        }

        private sealed record UnsupportedTarget : FilterTarget
        {
            public override FilterTargetFamily Family => FilterTargetFamily.FileContents;
        }
    }
}
