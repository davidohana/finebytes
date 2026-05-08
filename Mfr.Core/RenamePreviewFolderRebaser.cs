using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Core
{
    /// <summary>
    /// Rebases descendant items' preview directory paths so they follow ancestor folders being renamed in the same batch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In this context, a "descendant item" means any item whose preview parent directory points to
    /// the renamed folder itself or a path under it.
    /// </para>
    /// <para>
    /// Example: if folder <c>A</c> renames to <c>A2</c> and file <c>A\track.mp3</c> is also renamed
    /// to <c>A\song.mp3</c> in the same batch, the file preview still starts under <c>A</c>. This helper
    /// rewrites the file item's <c>Preview.DirectoryPath</c> from <c>A</c> to <c>A2</c> (and similarly for deeper paths).
    /// Without this rebase, a later file commit would either fail because the source path is no longer
    /// there or write to a stale location.
    /// </para>
    /// <para>
    /// Only folder items whose preview path differs from the original participate as rebase sources.
    /// Items with preview errors are left alone so the conflict detector can still surface their state.
    /// </para>
    /// </remarks>
    internal static class RenamePreviewFolderRebaser
    {
        /// <summary>
        /// Rebases descendant items' preview directory paths against in-batch ancestor renames.
        /// </summary>
        /// <remarks>
        /// <para>
        /// "Descendant" here is based on path containment of <see cref="RenameItem.Preview"/> directory:
        /// if <c>item.Preview.DirectoryPath</c> equals a renamed folder's original path or is under it,
        /// the path is rewritten to the folder's preview path.
        /// </para>
        /// </remarks>
        /// <param name="items">All rename items participating in the current preview pass.</param>
        internal static void RebaseDescendants(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var folderRenames = _CollectFolderRenames(items);
            if (folderRenames.Count == 0)
            {
                return;
            }

            // Process items shallow-first so each ancestor's Preview.FullPath is finalized
            // before any descendant references it during its own rebase.
            items
                .OrderBy(item => item.Original.FullPath.Length)
                .Where(item => item.Status != RenameStatus.PreviewError)
                .ToList()
                .ForEach(item => _RebaseItemAgainstAncestors(item, folderRenames));
        }

        /// <summary>
        /// Collects folder items that actually rename path in the current preview pass.
        /// </summary>
        /// <param name="items">All previewed rename items for the batch.</param>
        /// <returns>
        /// A list of directory items whose <see cref="RenameItem.Preview"/> full path differs from
        /// <see cref="RenameItem.Original"/> by ordinal comparison.
        /// </returns>
        /// <remarks>
        /// These items are used as ancestor rebase sources. Directory items without a path change are excluded.
        /// </remarks>
        private static List<RenameItem> _CollectFolderRenames(IReadOnlyList<RenameItem> items)
        {
            return [
                .. items
                .Where(item => item.Original.Attributes.IsDirectory())
                .Where(item => !item.IsPreviewPathUnchanged()),
            ];
        }

        /// <summary>
        /// Rebases one item's preview directory path against all renamed ancestor folders.
        /// </summary>
        /// <param name="item">The item whose <see cref="RenameItem.Preview"/> directory path may be rewritten.</param>
        /// <param name="folderRenames">
        /// Folder rename items that act as ancestor sources (original path to preview path).
        /// </param>
        /// <remarks>
        /// Ancestors are applied innermost-first so nested folder renames compose correctly.
        /// </remarks>
        private static void _RebaseItemAgainstAncestors(RenameItem item, IReadOnlyList<RenameItem> folderRenames)
        {
            // Apply ancestor renames innermost-first so a chain "A/B/C" is rewritten via B before A.
            var ancestorsInnermostFirst = folderRenames
                .Where(ancestor => !ReferenceEquals(ancestor, item))
                .Where(ancestor => _IsPreviewDirectoryUnderAncestor(
                    previewDirectoryPath: item.Preview.DirectoryPath,
                    ancestorOriginalPath: ancestor.Original.FullPath))
                .OrderByDescending(ancestor => ancestor.Original.FullPath.Length);

            foreach (var ancestor in ancestorsInnermostFirst)
            {
                var rebased = PathRelations.ReplaceAncestor(
                    fullPath: item.Preview.DirectoryPath,
                    oldAncestor: ancestor.Original.FullPath,
                    newAncestor: ancestor.Preview.FullPath);
                var pathChanged = !string.Equals(rebased, item.Preview.DirectoryPath, StringComparison.Ordinal);
                if (pathChanged)
                {
                    item.Preview.DirectoryPath = rebased;
                }
            }
        }

        private static bool _IsPreviewDirectoryUnderAncestor(string previewDirectoryPath, string ancestorOriginalPath)
        {
            return PathRelations.IsSamePath(first: previewDirectoryPath, second: ancestorOriginalPath)
                || PathRelations.IsDescendantOf(candidate: previewDirectoryPath, ancestor: ancestorOriginalPath);
        }
    }
}
