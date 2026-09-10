using Mfr.Utils;

namespace Mfr.Engine.Preview
{
    /// <summary>
    /// Applies in-batch folder-rename rewrites to a path, innermost ancestor first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by <see cref="RenamePreviewFolderRebaser"/> (preview directory paths) and
    /// <see cref="CommitPlanner"/> (on-disk source paths) so compose order and
    /// <see cref="PathRelations.ReplaceAncestor"/> stay aligned.
    /// </para>
    /// <para>
    /// Call sites must pass an explicit match dialect: preview rebase treats the renamed folder
    /// itself as a match (<see cref="PathRelations.IsSamePath(string, string)"/> or descendant); commit planning
    /// uses strict descendant matching on the item's original full path.
    /// Do not silently unify those dialects.
    /// </para>
    /// </remarks>
    internal static class InnermostAncestorRewrites
    {
        /// <summary>
        /// Rewrites <paramref name="path"/> through matching folder renames, innermost first.
        /// </summary>
        /// <param name="path">Absolute path to rewrite (match dialect is evaluated against this value before any rewrite).</param>
        /// <param name="folderRenames">Folder rename items whose original path maps to a preview path.</param>
        /// <param name="skipItem">Item whose path is being rewritten; never treated as its own ancestor.</param>
        /// <param name="matchesAncestor">
        /// Call-site selector: whether <paramref name="path"/> should receive a rewrite from the given ancestor original path.
        /// </param>
        /// <returns>
        /// The path after applying all matching ancestor renames innermost-first; unchanged when none match.
        /// </returns>
        internal static string Apply(
            string path,
            IReadOnlyList<RenameItem> folderRenames,
            RenameItem skipItem,
            Func<string, string, bool> matchesAncestor
        )
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(folderRenames);
            ArgumentNullException.ThrowIfNull(skipItem);
            ArgumentNullException.ThrowIfNull(matchesAncestor);

            if (folderRenames.Count == 0)
            {
                return path;
            }

            // Match against the pre-rewrite path, then apply ReplaceAncestor innermost-first so
            // nested chains (A/B/C) compose via B before A — same order for rebase and planner.
            var ancestorsInnermostFirst = folderRenames
                .Where(ancestor => !ReferenceEquals(ancestor, skipItem))
                .Where(ancestor => matchesAncestor(path, ancestor.Original.FullPath))
                .OrderByDescending(ancestor => ancestor.Original.FullPath.Length);

            var rewritten = path;
            foreach (var ancestor in ancestorsInnermostFirst)
            {
                rewritten = PathRelations.ReplaceAncestor(
                    fullPath: rewritten,
                    oldAncestor: ancestor.Original.FullPath,
                    newAncestor: ancestor.Preview.FullPath
                );
            }

            return rewritten;
        }
    }
}
