using Mfr.Utils;

namespace Mfr.Engine.Preview
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
    internal static class PreviewConflictDetector
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
            var folderRenameAncestors = PreviewFolderPathChanges.Collect(candidateItems);
            var duplicateDestinations = _BuildDuplicateDestinationSet(candidateItems);
            var originalPaths = candidateItems.Select(item => item.Original.FullPath).ToHashSet(PathComparers.Os);

            foreach (var item in candidateItems)
            {
                var destinationPath = item.Preview.FullPath;
                var isDuplicateDestination = duplicateDestinations.Contains(destinationPath);
                if (isDuplicateDestination)
                {
                    item.SetPreviewError(
                        message: $"More than one rename targets the same path '{destinationPath}'.",
                        cause: null
                    );
                    continue;
                }

                // No rename happens for this item; the file existing at its own path is not a conflict.
                // Case-only renames are still a "real change" so they fall through to occupancy checks below.
                if (item.IsPreviewPathUnchanged())
                {
                    continue;
                }

                var willBeVacatedByBatch = BatchDestinationVacate.WillBeVacated(
                    destinationPath: destinationPath,
                    movingSourcePaths: movingSourcePaths,
                    folderRenameAncestors: folderRenameAncestors
                );
                if (willBeVacatedByBatch)
                {
                    continue;
                }

                // A case-only rename targets the item's own path on a case-insensitive filesystem;
                // File.Move and Directory.Move accept this on .NET, so it's not a conflict.
                var isCaseOnlySelfRename = PathRelations.DiffersOnlyInCase(item.Original.FullPath, destinationPath);
                if (isCaseOnlySelfRename)
                {
                    continue;
                }

                var destinationOccupiedOnDisk = _DestinationOccupied(
                    destinationPath: destinationPath,
                    originalPaths: originalPaths
                );
                if (destinationOccupiedOnDisk)
                {
                    item.SetPreviewError(
                        message: $"Destination '{destinationPath}' is already in use (not vacated by another rename item in this batch).",
                        cause: null
                    );
                }
            }
        }

        /// <summary>
        /// Returns whether <paramref name="destinationPath"/> is already used, preferring in-list
        /// original paths so preview does not call <see cref="File.Exists"/> once per row.
        /// </summary>
        private static bool _DestinationOccupied(string destinationPath, HashSet<string> originalPaths)
        {
            if (originalPaths.Contains(destinationPath))
            {
                return true;
            }

            return Directory.Exists(destinationPath) || File.Exists(destinationPath);
        }

        private static HashSet<string> _BuildMovingSourceSet(IReadOnlyList<RenameItem> candidateItems)
        {
            return candidateItems
                .Where(item => !item.IsPreviewPathUnchanged())
                .Select(item => item.Original.FullPath)
                .ToHashSet(PathComparers.Os);
        }

        private static HashSet<string> _BuildDuplicateDestinationSet(IReadOnlyList<RenameItem> candidateItems)
        {
            return candidateItems
                .GroupBy(item => item.Preview.FullPath, PathComparers.Os)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(PathComparers.Os);
        }
    }
}
