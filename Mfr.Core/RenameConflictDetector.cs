using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Core
{
    /// <summary>
    /// Surfaces preview-time conflicts that block a clean batch commit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Detection runs on items whose preview succeeded; items already in <see cref="RenameStatus.PreviewError"/>
    /// state are left alone. New conflicts mark the item via <see cref="RenameItem.SetPreviewError"/>.
    /// </para>
    /// <para>
    /// Path comparisons honor the host filesystem's case sensitivity via <see cref="PathComparers.Os"/>,
    /// so two previews like <c>D:\dst\foo</c> and <c>D:\dst\Foo</c> are recognized as duplicates on Windows.
    /// </para>
    /// </remarks>
    internal static class RenameConflictDetector
    {
        /// <summary>
        /// Marks each rename item with preview-time conflicts that would prevent commit.
        /// </summary>
        /// <param name="items">All rename items participating in the current preview pass.</param>
        internal static void MarkConflicts(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var candidateItems = items.Where(item => item.Status == RenameStatus.PreviewOk).ToList();
            if (candidateItems.Count == 0)
            {
                return;
            }

            var movingSourcePaths = _BuildMovingSourceSet(candidateItems);
            var folderRenameAncestors = _BuildFolderRenameList(candidateItems);
            var duplicateDestinations = _BuildDuplicateDestinationSet(candidateItems);

            foreach (var item in candidateItems)
            {
                var destinationPath = item.Preview.FullPath;
                var isDuplicateDestination = duplicateDestinations.Contains(destinationPath);
                if (isDuplicateDestination)
                {
                    item.SetPreviewError(
                        message: $"More than one rename targets the same path '{destinationPath}'.",
                        cause: null);
                    continue;
                }

                // No rename happens for this item; the file existing at its own path is not a conflict.
                // Case-only renames are still a "real change" so they fall through to occupancy checks below.
                if (item.IsPreviewPathUnchanged())
                {
                    continue;
                }

                var willBeVacatedByBatch = _WillBeVacatedByBatch(
                    destinationPath: destinationPath,
                    movingSourcePaths: movingSourcePaths,
                    folderRenameAncestors: folderRenameAncestors);
                if (willBeVacatedByBatch)
                {
                    continue;
                }

                // A case-only rename targets the item's own path on a case-insensitive filesystem;
                // File.Move and Directory.Move accept this on .NET, so it's not a conflict.
                var isCaseOnlySelfRename = PathRelations.DiffersOnlyInCase(
                    item.Original.FullPath,
                    destinationPath);
                if (isCaseOnlySelfRename)
                {
                    continue;
                }

                var destinationOccupiedOnDisk =
                    Directory.Exists(destinationPath) || File.Exists(destinationPath);
                if (destinationOccupiedOnDisk)
                {
                    item.SetPreviewError(
                        message: $"Destination '{destinationPath}' is already in use (not vacated by another rename item in this batch).",
                        cause: null);
                    continue;
                }
            }
        }

        private static HashSet<string> _BuildMovingSourceSet(IReadOnlyList<RenameItem> candidateItems)
        {
            return candidateItems
                .Where(item => !item.IsPreviewPathUnchanged())
                .Select(item => item.Original.FullPath)
                .ToHashSet(PathComparers.Os);
        }

        private static List<RenameItem> _BuildFolderRenameList(IReadOnlyList<RenameItem> candidateItems)
        {
            return [.. candidateItems.Where(item => item.Original.Attributes.IsDirectory() && !item.IsPreviewPathUnchanged())];
        }

        private static HashSet<string> _BuildDuplicateDestinationSet(IReadOnlyList<RenameItem> candidateItems)
        {
            return candidateItems
                .GroupBy(item => item.Preview.FullPath, PathComparers.Os)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(PathComparers.Os);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="destinationPath"/> will be free by the time
        /// the batch commits, so occupying it is not a conflict.
        /// </summary>
        /// <param name="destinationPath">The preview destination path to test.</param>
        /// <param name="movingSourcePaths">
        /// Paths that are moving away from their current location during this batch.
        /// A destination that is itself a moving source will be vacated before it is claimed.
        /// </param>
        /// <param name="folderRenameAncestors">
        /// Folders in this batch that are being renamed to a different path.
        /// Any path that is a descendant of one of these folders is implicitly vacated when
        /// the ancestor folder moves.
        /// </param>
        private static bool _WillBeVacatedByBatch(
            string destinationPath,
            HashSet<string> movingSourcePaths,
            IReadOnlyList<RenameItem> folderRenameAncestors)
        {
            if (movingSourcePaths.Contains(destinationPath))
            {
                return true;
            }

            // A descendant path is implicitly vacated when its ancestor folder is renamed away.
            foreach (var folderRename in folderRenameAncestors)
            {
                var ancestorOriginalPath = folderRename.Original.FullPath;
                var destinationIsUnderRenamedAncestor = PathRelations.IsDescendantOf(
                    candidate: destinationPath,
                    ancestor: ancestorOriginalPath);
                if (destinationIsUnderRenamedAncestor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
