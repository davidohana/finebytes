using Mfr.Core;
using Mfr.Models;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="RenamePreviewFolderRebaser"/>.
    /// </summary>
    public sealed class RenamePreviewFolderRebaserTests
    {
        private static string Root => OperatingSystem.IsWindows() ? @"C:\" : "/";

        /// <summary>
        /// Verifies an item whose preview directory equals a renamed folder's original path is rebased.
        /// </summary>
        [Fact]
        public void RebaseDescendants_same_directory_as_ancestor_rebases_to_preview_path()
        {
            var oldFolderPath = _Path("A");
            var newFolderPath = _Path("A2");
            var folderItem = _CreateDirectoryItem(fullPath: oldFolderPath, previewFullPath: newFolderPath);

            var fileItem = _CreateFileItem(
                fullPath: _Path("A", "track.txt"),
                previewDirectoryPath: oldFolderPath,
                previewFileName: "song.txt");

            RenamePreviewFolderRebaser.RebaseDescendants([folderItem, fileItem]);

            Assert.Equal(newFolderPath, fileItem.Preview.DirectoryPath);
            Assert.Equal(_Path("A2", "song.txt"), fileItem.Preview.FullPath);
        }

        /// <summary>
        /// Verifies a strict descendant preview path is rebased under the renamed ancestor path.
        /// </summary>
        [Fact]
        public void RebaseDescendants_descendant_directory_rebases_under_renamed_ancestor()
        {
            var oldFolderPath = _Path("A");
            var newFolderPath = _Path("A2");
            var folderItem = _CreateDirectoryItem(fullPath: oldFolderPath, previewFullPath: newFolderPath);

            var fileItem = _CreateFileItem(
                fullPath: _Path("A", "sub", "track.txt"),
                previewDirectoryPath: _Path("A", "sub"),
                previewFileName: "song.txt");

            RenamePreviewFolderRebaser.RebaseDescendants([folderItem, fileItem]);

            Assert.Equal(_Path("A2", "sub"), fileItem.Preview.DirectoryPath);
            Assert.Equal(_Path("A2", "sub", "song.txt"), fileItem.Preview.FullPath);
        }

        /// <summary>
        /// Verifies unrelated folder renames do not rebase an item's preview directory path.
        /// </summary>
        [Fact]
        public void RebaseDescendants_unrelated_folder_rename_does_not_change_preview_directory()
        {
            var unrelatedFolder = _CreateDirectoryItem(
                fullPath: _Path("A"),
                previewFullPath: _Path("A2"));

            var fileItem = _CreateFileItem(
                fullPath: _Path("B", "track.txt"),
                previewDirectoryPath: _Path("B"),
                previewFileName: "song.txt");
            var expectedDirectory = fileItem.Preview.DirectoryPath;
            var expectedPath = fileItem.Preview.FullPath;

            RenamePreviewFolderRebaser.RebaseDescendants([unrelatedFolder, fileItem]);

            Assert.Equal(expectedDirectory, fileItem.Preview.DirectoryPath);
            Assert.Equal(expectedPath, fileItem.Preview.FullPath);
        }

        /// <summary>
        /// Verifies nested ancestor renames compose innermost-first for descendant rebasing.
        /// </summary>
        [Fact]
        public void RebaseDescendants_nested_ancestors_compose_innermost_first()
        {
            var outerFolderItem = _CreateDirectoryItem(
                fullPath: _Path("A"),
                previewFullPath: _Path("A2"));
            var innerFolderItem = _CreateDirectoryItem(
                fullPath: _Path("A", "B"),
                previewFullPath: _Path("A2", "B2"));

            var fileItem = _CreateFileItem(
                fullPath: _Path("A", "B", "sub", "track.txt"),
                previewDirectoryPath: _Path("A", "B", "sub"),
                previewFileName: "song.txt");

            RenamePreviewFolderRebaser.RebaseDescendants([outerFolderItem, innerFolderItem, fileItem]);

            Assert.Equal(_Path("A2", "B2", "sub"), fileItem.Preview.DirectoryPath);
            Assert.Equal(_Path("A2", "B2", "sub", "song.txt"), fileItem.Preview.FullPath);
        }

        /// <summary>
        /// Verifies items already in preview-error state are skipped by the rebaser.
        /// </summary>
        [Fact]
        public void RebaseDescendants_preview_error_item_is_not_rebased()
        {
            var folderItem = _CreateDirectoryItem(
                fullPath: _Path("A"),
                previewFullPath: _Path("A2"));

            var fileItem = _CreateFileItem(
                fullPath: _Path("A", "track.txt"),
                previewDirectoryPath: _Path("A"),
                previewFileName: "song.txt");
            fileItem.SetPreviewError(message: "preexisting", cause: null);
            var expectedDirectory = fileItem.Preview.DirectoryPath;
            var expectedPath = fileItem.Preview.FullPath;

            RenamePreviewFolderRebaser.RebaseDescendants([folderItem, fileItem]);

            Assert.Equal(expectedDirectory, fileItem.Preview.DirectoryPath);
            Assert.Equal(expectedPath, fileItem.Preview.FullPath);
            Assert.Equal(RenameStatus.PreviewError, fileItem.Status);
        }

        private static RenameItem _CreateDirectoryItem(string fullPath, string previewFullPath)
        {
            var item = _CreateItem(
                fullPath: fullPath,
                attributes: FileAttributes.Directory);
            _SetPreviewPath(item, previewFullPath);
            item.Status = RenameStatus.PreviewOk;
            return item;
        }

        private static RenameItem _CreateFileItem(
            string fullPath,
            string previewDirectoryPath,
            string previewFileName)
        {
            var item = _CreateItem(
                fullPath: fullPath,
                attributes: FileAttributes.Normal);
            item.Preview.DirectoryPath = previewDirectoryPath;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(previewFileName);
            item.Preview.Extension = Path.GetExtension(previewFileName);
            item.Status = RenameStatus.PreviewOk;
            return item;
        }

        private static RenameItem _CreateItem(string fullPath, FileAttributes attributes)
        {
            var meta = new FileMeta(
                globalIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetDirectoryName(fullPath)!,
                prefix: Path.GetFileNameWithoutExtension(fullPath),
                extension: Path.GetExtension(fullPath),
                attributes: attributes);
            return new RenameItem(meta);
        }

        private static void _SetPreviewPath(RenameItem item, string previewFullPath)
        {
            item.Preview.DirectoryPath = Path.GetDirectoryName(previewFullPath)!;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(previewFullPath);
            item.Preview.Extension = Path.GetExtension(previewFullPath);
        }

        private static string _Path(params string[] segments)
        {
            return Path.Combine([Root, .. segments]);
        }
    }
}
