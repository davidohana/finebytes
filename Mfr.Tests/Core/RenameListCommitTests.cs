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

            var preset = _CreatePresetAllEnabled(
                "duplicate",
                new FormatterFilter(
                    Target: new FileFullNameTarget(),
                    Options: new FormatterOptions("same.mp3")));
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var failingPreset = _FailingReplacerUnsupportedTargetPreset("failing-preview");
            _SetupPreview(renameList, failingPreset);
            var previewError = files[0].PreviewError;
            Assert.NotNull(previewError);
            Assert.NotNull(previewError.Cause);
            Assert.Equal(previewError.Cause.Message, previewError.Message);

            var successPreset = _CreateEmptyStepsPreset("successful-preview");
            _SetupPreview(renameList, successPreset);
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

            var preset = _CreateEmptyStepsPreset("no-change");
            _SetupPreview(renameList, preset);
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

            var preset = _CreateEmptyStepsPreset("no-change");
            _SetupPreview(renameList, preset);
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

            var preset = _CounterReplacePrefixPreset("counter");
            _SetupPreview(renameList, preset);
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

            var preset = _PrefixChainShiftBToCThenAToBPreset("chain-shift");
            _SetupPreview(renameList, preset);
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
        /// Mirrors <see cref="Commit_AllowsExistingDestination_WhenItWillBeRenamedAway"/> for directories: the second folder’s preview targets a path that still exists until the first moves away.
        /// </summary>
        public void Commit_AllowsExistingFolderDestination_WhenItWillBeRenamedAway()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourceFolderB = dir.CombinePath("b");
            var sourceFolderA = dir.CombinePath("a");
            var destinationFolderC = dir.CombinePath("c");
            Directory.CreateDirectory(sourceFolderB);
            Directory.CreateDirectory(sourceFolderA);
            File.WriteAllText(sourceFolderB.CombinePath("inside-b.txt"), "x");
            File.WriteAllText(sourceFolderA.CombinePath("inside-a.txt"), "y");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(
                sources: [sourceFolderB, sourceFolderA],
                includeFiles: false,
                includeFolders: true);
            var items = renameList.RenameItems;
            Assert.Equal(2, items.Count);

            var preset = _PrefixChainShiftBToCThenAToBPreset("folder-chain-shift");
            _SetupPreview(renameList, preset);
            Assert.DoesNotContain(items, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);
            Assert.Equal(2, result.Count(x => x.Status == RenameStatus.CommitOk));
            Assert.DoesNotContain(result, item => item.Status == RenameStatus.CommitError);
            Assert.False(Directory.Exists(dir.CombinePath("a")));

            var renamedFromAPath = dir.CombinePath("b");
            Assert.False(File.Exists(renamedFromAPath.CombinePath("inside-b.txt")));
            Assert.True(File.Exists(renamedFromAPath.CombinePath("inside-a.txt")));

            Assert.True(File.Exists(destinationFolderC.CombinePath("inside-b.txt")));
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

            var preset = _SetHiddenAttributesPreset("attrs");
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Hidden));

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "Attributes");

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

            var preset = _SetHiddenAttributesPreset("attrs-dry");
            _SetupPreview(renameList, preset);

            var result = renameList.Commit(failFast: false, dryRun: true);
            _AssertSingleCommitOk(result);

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

            var preset = _LastWriteDateSetterPreset("last-write", DateOnly.FromDateTime(before.AddDays(30)));
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.NotEqual(before, item.Preview.LastWriteTime);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "LastWriteTime");

            var after = File.GetLastWriteTime(path);
            Assert.Equal(item.Preview.LastWriteTime, after);
        }

        [Fact]
        /// <summary>
        /// Verifies that last-write time preview commits apply <see cref="TimeShifterFilter"/> shifts.
        /// </summary>
        public void Commit_LastWriteTimeOnly_AppliesTimeShifter()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("shifted.txt");
            File.WriteAllText(path, "x");
            var before = File.GetLastWriteTime(path);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);

            var preset = _LastWriteTimeShifterDaysPreset("time-shifter", days: 7);
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.Equal(before.AddDays(7), item.Preview.LastWriteTime);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "LastWriteTime");

            var after = File.GetLastWriteTime(path);
            Assert.Equal(item.Preview.LastWriteTime, after);
        }

        [Fact]
        /// <summary>
        /// Verifies commit creates missing destination directories when the preview path uses a new parent folder segment.
        /// </summary>
        public void Commit_CreatesFolders_WhenMovingFileToRenamedParentSegment()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldParent = dir.CombinePath("OldParent");
            Directory.CreateDirectory(oldParent);
            var filePath = oldParent.CombinePath("song.mp3");
            File.WriteAllText(filePath, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([filePath]);

            var preset = _RenameOldParentToNewParentPreset("parent-rename");
            _SetupPreview(renameList, preset);
            var expectedDest = Path.Combine(dir, "NewParent", "song.mp3");
            Assert.Equal(expectedDest, renameList.RenameItems[0].Preview.FullPath);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result);
            Assert.False(File.Exists(filePath));
            Assert.True(File.Exists(expectedDest));
        }

        [Fact]
        /// <summary>
        /// Verifies dry-run does not create folders for a preview that points at a missing parent path.
        /// </summary>
        public void Commit_DryRun_DoesNotCreateFolder_ForRenamedParentSegment()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldParent = dir.CombinePath("OldParent");
            Directory.CreateDirectory(oldParent);
            var filePath = oldParent.CombinePath("song.mp3");
            File.WriteAllText(filePath, "x");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([filePath]);

            var preset = _RenameOldParentToNewParentPreset("parent-rename-dry");
            _SetupPreview(renameList, preset);
            var expectedNewParent = dir.CombinePath("NewParent");
            Assert.False(Directory.Exists(expectedNewParent));

            var result = renameList.Commit(failFast: false, dryRun: true);
            _AssertSingleCommitOk(result);
            Assert.False(Directory.Exists(expectedNewParent));
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        /// <summary>
        /// Verifies commit moves the file when the preview assigns a new parent directory via <see cref="ParentDirectoryTarget"/>.
        /// </summary>
        public void Commit_MovesFile_WhenPreviewUsesChangedParentDirectory()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var originalParent = dir.CombinePath("FromHere");
            Directory.CreateDirectory(originalParent);
            var filePath = originalParent.CombinePath("song.mp3");
            File.WriteAllText(filePath, "x");

            var archivedParent = dir.CombinePath("Archived");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([filePath]);

            var preset = _FormatterParentDirectoryPreset("parent-dir-move", archivedParent);
            _SetupPreview(renameList, preset);
            var expectedDest = archivedParent.CombinePath("song.mp3");
            Assert.Equal(expectedDest, renameList.RenameItems[0].Preview.FullPath);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result);
            Assert.False(File.Exists(filePath));
            Assert.True(File.Exists(expectedDest));
        }

        [Fact]
        /// <summary>
        /// Verifies commit moves the file when the preview assigns a new absolute path via <see cref="FullPathTarget"/>.
        /// </summary>
        public void Commit_MovesFile_WhenPreviewUsesChangedFullPath()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var originalParent = dir.CombinePath("In");
            Directory.CreateDirectory(originalParent);
            var filePath = originalParent.CombinePath("song.mp3");
            File.WriteAllText(filePath, "x");

            var destinationFullPath = dir.CombinePath("Out", "song.mp3");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([filePath]);

            var preset = _FormatterFullPathPreset("full-path-move", destinationFullPath);
            _SetupPreview(renameList, preset);
            Assert.Equal(destinationFullPath, renameList.RenameItems[0].Preview.FullPath);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result);
            Assert.False(File.Exists(filePath));
            Assert.True(File.Exists(destinationFullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies commit renames an included directory item (same path-move path as files, via <see cref="FullPathTarget"/>).
        /// </summary>
        public void Commit_RenamesIncludedDirectory_WhenPreviewUsesChangedFullPath()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var originalFolderPath = dir.CombinePath("Album");
            Directory.CreateDirectory(originalFolderPath);
            var nestedFilePath = originalFolderPath.CombinePath("track.txt");
            File.WriteAllText(nestedFilePath, "nested");

            var destinationFolderPath = dir.CombinePath("AlbumRenamed");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: originalFolderPath,
                includeFiles: false,
                includeFolders: true);
            Assert.Single(renameList.RenameItems);

            var preset = _FormatterFullPathPreset("folder-full-path-move", destinationFolderPath);
            _SetupPreview(renameList, preset);
            Assert.Equal(destinationFolderPath, renameList.RenameItems[0].Preview.FullPath);
            Assert.DoesNotContain(renameList.RenameItems, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result);
            Assert.False(Directory.Exists(originalFolderPath));
            Assert.True(Directory.Exists(destinationFolderPath));
            Assert.True(File.Exists(destinationFolderPath.CombinePath("track.txt")));
            Assert.False(File.Exists(nestedFilePath));
        }

        [Fact]
        /// <summary>
        /// Verifies attribute-only preview commits call <see cref="File.SetAttributes"/> on an included directory path.
        /// </summary>
        public void Commit_AttributesOnly_AppliesHidden_OnIncludedDirectory()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var folderPath = dir.CombinePath("folder");
            Directory.CreateDirectory(folderPath);
            var attrsBefore = File.GetAttributes(folderPath);
            Assert.False(attrsBefore.HasFlag(FileAttributes.Hidden));
            Assert.True(attrsBefore.HasFlag(FileAttributes.Directory));

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: folderPath,
                includeFiles: false,
                includeFolders: true);
            Assert.Single(renameList.RenameItems);
            Assert.True(renameList.RenameItems[0].Original.Attributes.HasFlag(FileAttributes.Directory));

            var preset = _SetHiddenAttributesPreset("attrs-folder");
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Hidden));
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Directory));

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "Attributes");

            var attrsAfter = File.GetAttributes(folderPath);
            Assert.True(attrsAfter.HasFlag(FileAttributes.Hidden));
            Assert.True(attrsAfter.HasFlag(FileAttributes.Directory));
        }

        [Fact]
        /// <summary>
        /// Verifies dry-run commit skips attribute writes for an included directory.
        /// </summary>
        public void Commit_AttributesOnly_DryRun_LeavesAttributes_OnIncludedDirectory()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var folderPath = dir.CombinePath("folder-dry");
            Directory.CreateDirectory(folderPath);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: folderPath,
                includeFiles: false,
                includeFolders: true);

            var preset = _SetHiddenAttributesPreset("attrs-folder-dry");
            _SetupPreview(renameList, preset);

            var result = renameList.Commit(failFast: false, dryRun: true);
            _AssertSingleCommitOk(result);

            var attrsAfter = File.GetAttributes(folderPath);
            Assert.False(attrsAfter.HasFlag(FileAttributes.Hidden));
        }

        [Fact]
        /// <summary>
        /// Verifies last-write commits on an included directory use <see cref="File.SetLastWriteTime"/>.
        /// </summary>
        public void Commit_LastWriteTimeOnly_AppliesDateSetter_OnIncludedDirectory()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var folderPath = dir.CombinePath("dated-folder");
            Directory.CreateDirectory(folderPath);
            var before = File.GetLastWriteTime(folderPath);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: folderPath,
                includeFiles: false,
                includeFolders: true);

            var preset = _LastWriteDateSetterPreset(
                "last-write-folder",
                DateOnly.FromDateTime(before.AddDays(30)));
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.NotEqual(before, item.Preview.LastWriteTime);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "LastWriteTime");

            var after = File.GetLastWriteTime(folderPath);
            Assert.Equal(item.Preview.LastWriteTime, after);
        }

        [Fact]
        /// <summary>
        /// Verifies <see cref="TimeShifterFilter"/> last-write deltas commit for an included directory.
        /// </summary>
        public void Commit_LastWriteTimeOnly_AppliesTimeShifter_OnIncludedDirectory()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var folderPath = dir.CombinePath("shifted-folder");
            Directory.CreateDirectory(folderPath);
            var before = File.GetLastWriteTime(folderPath);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: folderPath,
                includeFiles: false,
                includeFolders: true);

            var preset = _LastWriteTimeShifterDaysPreset("time-shifter-folder", days: 7);
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.Equal(before.AddDays(7), item.Preview.LastWriteTime);

            var result = renameList.Commit(failFast: false);
            _AssertSingleCommitOk(result, expectChangeProperty: "LastWriteTime");

            var after = File.GetLastWriteTime(folderPath);
            Assert.Equal(item.Preview.LastWriteTime, after);
        }

        private static readonly CounterOptions _CounterReplacePrefixOptions = new(
            Start: 1,
            Step: 1,
            Width: 3,
            PadChar: "0",
            Position: CounterPosition.Replace,
            Separator: " - ",
            ResetPerFolder: false);

        private static FilterPreset _CreatePresetAllEnabled(string name, params BaseFilter[] filters)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = FilterChain.CreateAllEnabled(filters),
            };
        }

        private static FilterPreset _CreateEmptyStepsPreset(string name)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = new FilterChain { Steps = [] },
            };
        }

        private static FilterPreset _CounterReplacePrefixPreset(string name)
        {
            return _CreatePresetAllEnabled(
                name,
                new CounterFilter(Target: new FilePrefixTarget(), Options: _CounterReplacePrefixOptions));
        }

        private static FilterPreset _SetHiddenAttributesPreset(string name)
        {
            return _CreatePresetAllEnabled(
                name,
                new AttributesSetterFilter(
                    Options: new AttributesSetterOptions(
                        ReadOnly: AttributeTriState.Keep,
                        Hidden: AttributeTriState.Set,
                        Archive: AttributeTriState.Keep,
                        System: AttributeTriState.Keep)));
        }

        private static FilterPreset _LastWriteDateSetterPreset(string name, DateOnly date)
        {
            return _CreatePresetAllEnabled(
                name,
                new DateSetterFilter(
                    Options: new DateSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        Date: date)));
        }

        private static FilterPreset _LastWriteTimeShifterDaysPreset(string name, int days)
        {
            return _CreatePresetAllEnabled(
                name,
                new TimeShifterFilter(
                    Options: new TimeShifterOptions(
                        TimestampField: TimestampField.LastWrite,
                        Amount: days,
                        Unit: TimeShiftUnit.Days)));
        }

        private static FilterPreset _PrefixChainShiftBToCThenAToBPreset(string name)
        {
            return _CreatePresetAllEnabled(
                name,
                new ReplacerFilter(
                    Target: new FilePrefixTarget(),
                    Options: new ReplacerOptions(
                        "b",
                        "c",
                        ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: false,
                        WholeWord: false)),
                new ReplacerFilter(
                    Target: new FilePrefixTarget(),
                    Options: new ReplacerOptions(
                        "a",
                        "b",
                        ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: false,
                        WholeWord: false)));
        }

        private static FilterPreset _RenameOldParentToNewParentPreset(string name)
        {
            return _CreatePresetAllEnabled(
                name,
                new ReplacerFilter(
                    Target: new AncestorFolderTarget(Level: 1),
                    Options: new ReplacerOptions(
                        Find: "OldParent",
                        Replacement: "NewParent",
                        Mode: ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: false,
                        WholeWord: false)));
        }

        private static FilterPreset _FormatterParentDirectoryPreset(string name, string parentPath)
        {
            return _CreatePresetAllEnabled(
                name,
                new FormatterFilter(Target: new ParentDirectoryTarget(), Options: new FormatterOptions(parentPath)));
        }

        private static FilterPreset _FormatterFullPathPreset(string name, string fullPath)
        {
            return _CreatePresetAllEnabled(
                name,
                new FormatterFilter(Target: new FullPathTarget(), Options: new FormatterOptions(fullPath)));
        }

        private static void _SetupPreview(RenameList renameList, FilterPreset preset)
        {
            preset.Chain.SetupFilters();
            renameList.Preview(preset);
        }

        private static void _AssertSingleCommitOk(IReadOnlyList<RenameResultItem> result, string? expectChangeProperty = null)
        {
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);
            if (expectChangeProperty is not null)
            {
                Assert.Contains(result[0].Changes, c => c.Property == expectChangeProperty);
            }
        }

        private static FilterPreset _FailingReplacerUnsupportedTargetPreset(string name)
        {
            return _CreatePresetAllEnabled(
                name,
                new ReplacerFilter(
                    Target: new UnsupportedTarget(),
                    Options: new ReplacerOptions(
                        "a",
                        "b",
                        ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: true,
                        WholeWord: false)));
        }

        private sealed record UnsupportedTarget : FilterTarget;
    }
}
