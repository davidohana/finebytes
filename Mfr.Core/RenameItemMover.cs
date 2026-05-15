using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Core
{
    /// <summary>
    /// Performs filesystem move operations on <see cref="RenameItem"/> instances during commit.
    /// </summary>
    internal static class RenameItemMover
    {
        private const string _TempSuffixPrefix = ".mfrtmp-";

        /// <summary>
        /// When set, replaces <see cref="StashSourceToTemp"/> behavior for tests that assert stash failures.
        /// </summary>
        /// <remarks>
        /// Production code leaves this <c>null</c>; safer alternatives (filesystem mocking) would split IO behind injectable
        /// interfaces across many rename callers, which this helper avoids for one targeted assertion path.
        /// </remarks>
        internal static Action<RenameItem, string>? StashSourceToTempSubstitute;

        /// <summary>
        /// Moves an item's source to <paramref name="tempPath"/> without applying attribute or timestamp changes.
        /// </summary>
        /// <param name="item">The item whose source should be stashed.</param>
        /// <param name="tempPath">Unique temp destination path.</param>
        /// <remarks>
        /// <para>
        /// Used for cycle resolution and case-only renames. <see cref="RenameItem.Original"/> is not updated;
        /// callers must follow up with <see cref="FinalizeCommit"/> before the preview can be considered applied.
        /// </para>
        /// </remarks>
        internal static void StashSourceToTemp(RenameItem item, string tempPath)
        {
            ArgumentNullException.ThrowIfNull(tempPath);
            if (StashSourceToTempSubstitute is not null)
            {
                StashSourceToTempSubstitute(item, tempPath);
                return;
            }

            _MoveEntry(
                sourceFullPath: item.Original.FullPath,
                destinationFullPath: tempPath,
                sourceIsDirectory: item.Original.Attributes.IsDirectory());
        }

        /// <summary>
        /// Completes the commit for <paramref name="item"/> using <paramref name="actualSourcePath"/> as the current on-disk source.
        /// </summary>
        /// <param name="item">The item being committed.</param>
        /// <param name="actualSourcePath">
        /// The current on-disk path of the source.
        /// May be the item's original path, a stash temp path, or an ancestor-rebased path.
        /// </param>
        /// <remarks>
        /// <para>
        /// When <paramref name="actualSourcePath"/> already equals the preview path, the move is skipped
        /// and only attribute and timestamp changes are applied.
        /// </para>
        /// <para>
        /// Updates <see cref="RenameItem.Original"/> to match the applied preview on success.
        /// </para>
        /// </remarks>
        internal static void FinalizeCommit(RenameItem item, string actualSourcePath)
        {
            ArgumentNullException.ThrowIfNull(actualSourcePath);

            var pathDiffersFromPreview = !string.Equals(
                actualSourcePath,
                item.Preview.FullPath,
                StringComparison.Ordinal);
            if (pathDiffersFromPreview)
            {
                _EnsureDestinationParentExists(item.Preview.FullPath);
                _MoveEntry(
                    sourceFullPath: actualSourcePath,
                    destinationFullPath: item.Preview.FullPath,
                    sourceIsDirectory: item.Original.Attributes.IsDirectory());
            }

            var pathOnDisk = item.Preview.FullPath;
            if (item.Original.Attributes != item.Preview.Attributes)
                File.SetAttributes(pathOnDisk, item.Preview.Attributes);

            var pathIsDirectoryAfterApply = pathOnDisk.IsDirectory();
            _ApplyTimestampChangesIfNeeded(
                pathOnDisk: pathOnDisk,
                original: item.Original,
                preview: item.Preview,
                pathIsDirectory: pathIsDirectoryAfterApply);

            if (item.HasPreviewChanges())
                item.Original = item.Preview.Clone();

        }

        /// <summary>
        /// Allocates a unique temp path adjacent to <paramref name="nearPath"/> with the same parent.
        /// </summary>
        /// <param name="nearPath">A path whose parent directory should host the temp entry.</param>
        /// <returns>An unused absolute path suitable for stashing.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the GUID-based candidate path is already occupied.</exception>
        internal static string AllocateTempPath(string nearPath)
        {
            ArgumentNullException.ThrowIfNull(nearPath);

            var directory = Path.GetDirectoryName(nearPath) ?? string.Empty;
            var leaf = Path.GetFileName(Path.TrimEndingDirectorySeparator(nearPath));
            if (string.IsNullOrEmpty(leaf))
                leaf = "entry";

            var suffix = _TempSuffixPrefix + Guid.NewGuid().ToString("N");
            var candidate = string.IsNullOrEmpty(directory)
                ? leaf + suffix
                : Path.Combine(directory, leaf + suffix);

            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                throw new InvalidOperationException(
                    $"GUID-based temp path '{candidate}' is already occupied.");
            }

            return candidate;
        }

        private static void _EnsureDestinationParentExists(string destinationFullPath)
        {
            var destinationDirectoryPath = Path.GetDirectoryName(destinationFullPath);
            if (string.IsNullOrEmpty(destinationDirectoryPath))
            {
                throw new InvalidOperationException(
                    $"Cannot resolve a parent directory for destination path '{destinationFullPath}'.");
            }

            Directory.CreateDirectory(destinationDirectoryPath);
        }

        private static void _MoveEntry(string sourceFullPath, string destinationFullPath, bool sourceIsDirectory)
        {
            if (sourceIsDirectory)
            {
                Directory.Move(sourceFullPath, destinationFullPath);
                return;
            }

            File.Move(sourceFullPath, destinationFullPath, overwrite: false);
        }

        // Uses Directory.Set* vs File.Set* based on entry kind (Windows directory paths may reject File.* time setters).
        private static void _ApplyTimestampChangesIfNeeded(
            string pathOnDisk,
            FileMeta original,
            FileMeta preview,
            bool pathIsDirectory)
        {
            Action<string, DateTime> setCreationTime =
                pathIsDirectory ? Directory.SetCreationTime : File.SetCreationTime;
            Action<string, DateTime> setLastWriteTime =
                pathIsDirectory ? Directory.SetLastWriteTime : File.SetLastWriteTime;
            Action<string, DateTime> setLastAccessTime =
                pathIsDirectory ? Directory.SetLastAccessTime : File.SetLastAccessTime;

            if (original.CreationTime != preview.CreationTime)
                setCreationTime(pathOnDisk, preview.CreationTime);

            if (original.LastWriteTime != preview.LastWriteTime)
                setLastWriteTime(pathOnDisk, preview.LastWriteTime);

            if (original.LastAccessTime != preview.LastAccessTime)
                setLastAccessTime(pathOnDisk, preview.LastAccessTime);
        }
    }
}
