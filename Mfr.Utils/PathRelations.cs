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
        /// Whether two paths refer to the same location under host filesystem comparison, ignoring trailing separators.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <returns>
        /// <see langword="true"/> when both paths refer to the same location after trimming trailing separators.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Prefer this overload for directory identity. Use
        /// <see cref="IsSamePath(string, string, bool)"/> with <c>trimTrailingSeparators: false</c>
        /// when comparing exact path text (for example case-only rename detection).
        /// </para>
        /// </remarks>
        public static bool IsSamePath(string first, string second)
        {
            return IsSamePath(first: first, second: second, trimTrailingSeparators: true);
        }

        /// <summary>
        /// Whether two paths are equal under host filesystem comparison, optionally trimming trailing separators.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <param name="trimTrailingSeparators">
        /// When <see langword="true"/>, trailing directory separators are ignored before comparison.
        /// When <see langword="false"/>, path text is compared as-is under the OS comparer.
        /// </param>
        /// <returns><see langword="true"/> when the comparer treats both paths as the same.</returns>
        public static bool IsSamePath(string first, string second, bool trimTrailingSeparators)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            if (trimTrailingSeparators)
            {
                first = first.TrimTrailingSeparator();
                second = second.TrimTrailingSeparator();
            }

            return PathComparers.Os.Equals(first, second);
        }

        /// <summary>
        /// Whether two paths refer to the same on-disk entry but differ in textual casing.
        /// </summary>
        /// <param name="first">First absolute path.</param>
        /// <param name="second">Second absolute path.</param>
        /// <returns><c>true</c> when only character casing differs.</returns>
        /// <remarks>
        /// <para>
        /// Uses exact path-text equality (<c>trimTrailingSeparators: false</c>): a trailing-separator
        /// difference is not a case-only rename.
        /// </para>
        /// </remarks>
        public static bool DiffersOnlyInCase(string first, string second)
        {
            var samePathText = IsSamePath(first: first, second: second, trimTrailingSeparators: false);
            if (!samePathText)
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
        /// Whether <paramref name="path"/> is a drive or filesystem root after <see cref="Path.GetFullPath(string)"/>.
        /// </summary>
        /// <param name="path">Candidate file or directory path.</param>
        /// <returns>
        /// <see langword="true"/> when the full path equals its path root; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Used by Rename List add-source soft-reject (UI) and Engine hard-reject for the same root rule.
        /// Callers that must not throw (UI) should catch path-resolution failures; Engine lets them propagate.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> cannot be resolved by the host.</exception>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="path"/> is not supported on the host.</exception>
        /// <exception cref="IOException">Thrown when the host cannot resolve <paramref name="path"/>.</exception>
        public static bool IsFilesystemRoot(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);
            return !string.IsNullOrEmpty(root) && string.Equals(root, fullPath, PathComparers.OsComparison);
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
                separatorChar == Path.DirectorySeparatorChar || separatorChar == Path.AltDirectorySeparatorChar;
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
            var pathIsAncestorItself = IsSamePath(fullPath, oldAncestor);
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
