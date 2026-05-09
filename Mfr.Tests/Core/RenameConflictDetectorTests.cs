using Mfr.Core;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="RenameConflictDetector.MarkConflicts"/>.
    /// </summary>
    public sealed class RenameConflictDetectorTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies two items targeting the same destination are flagged as duplicates.
        /// </summary>
        [Fact]
        public void Duplicate_destinations_marked_as_preview_error()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstItem = _CreateFileItem(dir, "a.mp3");
            var secondItem = _CreateFileItem(dir, "b.mp3");
            _RetargetPreview(firstItem, dir, "same.mp3");
            _RetargetPreview(secondItem, dir, "same.mp3");

            RenameConflictDetector.MarkConflicts([firstItem, secondItem]);

            Assert.Equal(RenameStatus.PreviewError, firstItem.Status);
            Assert.Equal(RenameStatus.PreviewError, secondItem.Status);
            Assert.Contains("same path", firstItem.PreviewError!.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies items whose destination exists on disk but is not vacated by another rename are flagged.
        /// </summary>
        [Fact]
        public void Destination_occupied_on_disk_marks_preview_error()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("source.mp3");
            var occupiedDestPath = dir.CombinePath("occupied.mp3");
            File.WriteAllText(sourcePath, "x");
            File.WriteAllText(occupiedDestPath, "y");

            var item = _CreateItemFromExistingFile(sourcePath);
            _RetargetPreview(item, dir, "occupied.mp3");

            RenameConflictDetector.MarkConflicts([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Contains("already in use", item.PreviewError!.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies a destination vacated by another item in the same batch does not trigger a conflict.
        /// </summary>
        [Fact]
        public void Vacated_destination_in_batch_is_not_conflict()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var firstSourcePath = dir.CombinePath("first.mp3");
            var secondSourcePath = dir.CombinePath("second.mp3");
            File.WriteAllText(firstSourcePath, "1");
            File.WriteAllText(secondSourcePath, "2");

            var firstItem = _CreateItemFromExistingFile(firstSourcePath);
            var secondItem = _CreateItemFromExistingFile(secondSourcePath);
            _RetargetPreview(firstItem, dir, "second.mp3");
            _RetargetPreview(secondItem, dir, "third.mp3");

            RenameConflictDetector.MarkConflicts([firstItem, secondItem]);

            Assert.Null(firstItem.PreviewError);
            Assert.Null(secondItem.PreviewError);
        }

        /// <summary>
        /// Verifies items with no rename (preview path equals original) are not flagged when their own path exists on disk.
        /// </summary>
        [Fact]
        public void Item_without_changes_is_not_flagged_when_its_own_path_exists()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("unchanged.mp3");
            File.WriteAllText(sourcePath, "x");

            var item = _CreateItemFromExistingFile(sourcePath);
            item.Status = RenameStatus.PreviewOk;

            RenameConflictDetector.MarkConflicts([item]);

            Assert.Null(item.PreviewError);
        }

        /// <summary>
        /// Verifies a case-only rename on the host filesystem is not flagged as occupied.
        /// </summary>
        [Fact]
        public void Case_only_rename_is_not_flagged_as_occupied()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("track.mp3");
            File.WriteAllText(sourcePath, "x");

            var item = _CreateItemFromExistingFile(sourcePath);
            _RetargetPreview(item, dir, "Track.mp3");

            RenameConflictDetector.MarkConflicts([item]);

            Assert.Null(item.PreviewError);
        }

        /// <summary>
        /// Verifies items whose parent folder is being renamed in the same batch are not flagged
        /// just because the parent folder still exists on disk during preview.
        /// </summary>
        [Fact]
        public void Descendant_under_renamed_ancestor_is_not_flagged()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("Album");
            var newFolder = dir.CombinePath("AlbumRenamed");
            Directory.CreateDirectory(oldFolder);
            var nestedFilePath = oldFolder.CombinePath("track.mp3");
            File.WriteAllText(nestedFilePath, "x");

            var folderItem = _CreateDirectoryItem(dir, "Album");
            _RetargetPreview(folderItem, dir, "AlbumRenamed");

            var nestedItem = _CreateItemFromExistingFile(nestedFilePath);
            // Preview is rebased to the renamed parent folder.
            nestedItem.Preview.DirectoryPath = newFolder;

            RenameConflictDetector.MarkConflicts([folderItem, nestedItem]);

            Assert.Null(folderItem.PreviewError);
            Assert.Null(nestedItem.PreviewError);
        }

        /// <summary>
        /// Verifies items already in PreviewError state are left alone (status is not overwritten).
        /// </summary>
        [Fact]
        public void Items_already_in_preview_error_are_left_alone()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var item = _CreateFileItem(dir, "a.mp3");
            item.SetPreviewError("preexisting", null);

            RenameConflictDetector.MarkConflicts([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("preexisting", item.PreviewError!.Message);
        }

        private static RenameItem _CreateFileItem(string directoryPath, string fileName)
        {
            var prefix = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: prefix,
                extension: extension,
                attributes: FileAttributes.Normal);
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            return item;
        }

        private static RenameItem _CreateDirectoryItem(string directoryPath, string folderName)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: folderName,
                extension: string.Empty,
                attributes: FileAttributes.Directory);
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            return item;
        }

        private static RenameItem _CreateItemFromExistingFile(string sourcePath)
        {
            var directoryPath = Path.GetDirectoryName(sourcePath)!;
            var prefix = Path.GetFileNameWithoutExtension(sourcePath);
            var extension = Path.GetExtension(sourcePath);
            var attributes = File.Exists(sourcePath)
                ? FileAttributes.Normal
                : FileAttributes.Directory;
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: prefix,
                extension: extension,
                attributes: attributes);
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            return item;
        }

        private static void _RetargetPreview(RenameItem item, string directoryPath, string fileName)
        {
            item.Preview.DirectoryPath = directoryPath;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(fileName);
            item.Preview.Extension = Path.GetExtension(fileName);
        }
    }
}
