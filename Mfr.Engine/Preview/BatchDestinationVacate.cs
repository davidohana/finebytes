using Mfr.Utils;

namespace Mfr.Engine.Preview
{
    /// <summary>
    /// Shared rules for whether a preview destination will be free by commit time because another
    /// batch participant is moving away from that path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by <see cref="PreviewConflictDetector"/> (occupancy) and <see cref="Commit.CommitPlanner"/>
    /// (ordering edges) so preview-ok and commit planning stay on the same vacate policy.
    /// </para>
    /// <para>
    /// A destination is vacated when (1) it equals a moving participant's original path, or (2) it is a
    /// strict descendant of a folder participant whose preview path changed.
    /// </para>
    /// </remarks>
    internal static class BatchDestinationVacate
    {
        /// <summary>
        /// Returns whether <paramref name="destinationPath"/> will be free by the time the batch commits.
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
        /// <returns><c>true</c> when the destination is vacated by an exact move or a folder rename.</returns>
        internal static bool WillBeVacated(
            string destinationPath,
            HashSet<string> movingSourcePaths,
            IReadOnlyList<RenameItem> folderRenameAncestors
        )
        {
            ArgumentNullException.ThrowIfNull(destinationPath);
            ArgumentNullException.ThrowIfNull(movingSourcePaths);
            ArgumentNullException.ThrowIfNull(folderRenameAncestors);

            if (movingSourcePaths.Contains(destinationPath))
            {
                return true;
            }

            return IsVacatedByFolderRename(destinationPath, folderRenameAncestors);
        }

        /// <summary>
        /// Returns whether <paramref name="destinationPath"/> lies under a folder being renamed away.
        /// </summary>
        /// <param name="destinationPath">The preview destination path to test.</param>
        /// <param name="folderRenameAncestors">Folder participants whose preview path changed.</param>
        /// <returns><c>true</c> when any renamed folder is a strict ancestor of the destination.</returns>
        internal static bool IsVacatedByFolderRename(
            string destinationPath,
            IReadOnlyList<RenameItem> folderRenameAncestors
        )
        {
            ArgumentNullException.ThrowIfNull(destinationPath);
            ArgumentNullException.ThrowIfNull(folderRenameAncestors);

            return folderRenameAncestors.Any(folderRename =>
                PathRelations.IsDescendantOf(candidate: destinationPath, ancestor: folderRename.Original.FullPath)
            );
        }

        /// <summary>
        /// Yields folder renames that vacate <paramref name="destinationPath"/> by ancestor move.
        /// </summary>
        /// <param name="destinationPath">The preview destination path to test.</param>
        /// <param name="folderRenameAncestors">Folder participants whose preview path changed.</param>
        /// <returns>Folder items that must commit before a claimer of <paramref name="destinationPath"/>.</returns>
        internal static IEnumerable<RenameItem> FoldersThatVacate(
            string destinationPath,
            IReadOnlyList<RenameItem> folderRenameAncestors
        )
        {
            ArgumentNullException.ThrowIfNull(destinationPath);
            ArgumentNullException.ThrowIfNull(folderRenameAncestors);

            foreach (var folderRename in folderRenameAncestors)
            {
                if (PathRelations.IsDescendantOf(candidate: destinationPath, ancestor: folderRename.Original.FullPath))
                {
                    yield return folderRename;
                }
            }
        }
    }
}
