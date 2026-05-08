namespace Mfr.Utils
{
    /// <summary>
    /// Provides relational helpers for absolute filesystem paths used during rename planning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All inputs are expected to be normalized (e.g. via <see cref="Path.GetFullPath(string)"/>).
    /// Comparisons honor the host filesystem's case sensitivity unless an explicit comparer is provided.
    /// </para>
    /// </remarks>
    public static class PathRelations
    {
        /// <summary>
        /// Whether two paths refer to the same on-disk entry under the host filesystem.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <returns><c>true</c> when both paths normalize to the same on-disk entry.</returns>
        public static bool SameOnDisk(string first, string second)
        {
            return PathComparers.Os.Equals(first, second);
        }

        /// <summary>
        /// Whether two paths refer to the same on-disk entry but differ in textual casing.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <returns><c>true</c> when only character casing differs.</returns>
        public static bool DiffersOnlyInCase(string first, string second)
        {
            var sameOnDisk = SameOnDisk(first, second);
            if (!sameOnDisk)
            {
                return false;
            }

            return !string.Equals(first, second, StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> is a strict descendant of <paramref name="ancestor"/>.
        /// </summary>
        /// <para>
        /// A path is not its own descendant. Ancestor matching uses a directory separator boundary so
        /// <c>D:\foobar</c> is not treated as a descendant of <c>D:\foo</c>.
        /// </para>
        /// <param name="candidate">Absolute candidate path.</param>
        /// <param name="ancestor">Absolute ancestor directory path.</param>
        /// <returns><c>true</c> when <paramref name="candidate"/> lives strictly below <paramref name="ancestor"/>.</returns>
        public static bool IsDescendantOf(string candidate, string ancestor)
        {
            return IsDescendantOf(candidate: candidate, ancestor: ancestor, comparer: PathComparers.Os);
        }

        /// <summary>
        /// Whether two paths are the same directory under host filesystem comparison rules.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <returns><c>true</c> when both paths refer to the same directory path after trimming trailing separators.</returns>
        public static bool IsSamePath(string first, string second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            return PathComparers.Os.Equals(
                first.TrimTrailingSeparator(),
                second.TrimTrailingSeparator());
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> is a strict descendant of <paramref name="ancestor"/> using a custom comparer.
        /// </summary>
        /// <param name="candidate">Absolute candidate path.</param>
        /// <param name="ancestor">Absolute ancestor directory path.</param>
        /// <param name="comparer">Comparer used for the prefix match.</param>
        /// <returns><c>true</c> when the candidate path lies strictly under the ancestor path.</returns>
        public static bool IsDescendantOf(string candidate, string ancestor, StringComparer comparer)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(ancestor);
            ArgumentNullException.ThrowIfNull(comparer);

            var trimmedAncestor = ancestor.TrimTrailingSeparator();
            if (trimmedAncestor.Length == 0)
            {
                return false;
            }

            var trimmedCandidate = candidate.TrimTrailingSeparator();
            if (trimmedCandidate.Length <= trimmedAncestor.Length)
            {
                return false;
            }

            var separatorChar = trimmedCandidate[trimmedAncestor.Length];
            var nextCharIsSeparator =
                separatorChar == Path.DirectorySeparatorChar
                || separatorChar == Path.AltDirectorySeparatorChar;
            if (!nextCharIsSeparator)
            {
                return false;
            }

            var prefix = trimmedCandidate[..trimmedAncestor.Length];
            return comparer.Equals(prefix, trimmedAncestor);
        }

        /// <summary>
        /// Replaces an ancestor path prefix in <paramref name="fullPath"/> with <paramref name="newAncestor"/>.
        /// </summary>
        /// <param name="fullPath">Absolute path to rewrite.</param>
        /// <param name="oldAncestor">Existing ancestor path prefix.</param>
        /// <param name="newAncestor">Replacement ancestor path prefix.</param>
        /// <returns>The rewritten path; returns <paramref name="fullPath"/> unchanged when the prefix does not match.</returns>
        public static string ReplaceAncestor(string fullPath, string oldAncestor, string newAncestor)
        {
            ArgumentNullException.ThrowIfNull(fullPath);
            ArgumentNullException.ThrowIfNull(oldAncestor);
            ArgumentNullException.ThrowIfNull(newAncestor);

            var trimmedOld = oldAncestor.TrimTrailingSeparator();
            var pathIsAncestorItself = PathComparers.Os.Equals(fullPath.TrimTrailingSeparator(), trimmedOld);
            if (pathIsAncestorItself)
            {
                return newAncestor;
            }

            if (!IsDescendantOf(fullPath, oldAncestor))
            {
                return fullPath;
            }

            var remainder = fullPath[trimmedOld.Length..];
            return newAncestor.TrimTrailingSeparator() + remainder;
        }
    }
}
