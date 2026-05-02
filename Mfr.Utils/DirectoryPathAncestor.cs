using System.Diagnostics.CodeAnalysis;

namespace Mfr.Utils
{
    /// <summary>
    /// Resolves and rewrites ancestor folder-name segments counted from an absolute containing-directory path toward the volume root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetSegmentName"/> and <see cref="ReplaceSegment"/> walk toward the volume root via
    /// <see cref="M:System.IO.Path.GetDirectoryName(System.String)"/> and <see cref="M:System.IO.Path.GetFileName(System.String)"/>.
    /// </para>
    /// </remarks>
    public static class DirectoryPathAncestor
    {
        /// <summary>
        /// Returns one folder-name segment counted from the item’s containing directory upward.
        /// </summary>
        /// <para>
        /// Level <c>1</c> names the leaf folder (<paramref name="containingDirectoryPath"/>),
        /// <c>2</c> names its parent, and so on.
        /// </para>
        /// <param name="containingDirectoryPath">Absolute directory that holds the rename item.</param>
        /// <param name="level">Positive ancestor distance (<c>1</c>-based).</param>
        /// <returns>The folder segment name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is less than <c>1</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the path is too shallow for <paramref name="level"/>.</exception>
        public static string GetSegmentName(string containingDirectoryPath, int level)
        {
            _RequireLevel(level);
            var walker = Path.TrimEndingDirectorySeparator(containingDirectoryPath);

            for (var upwardStep = 1; upwardStep < level; upwardStep++)
            {
                var parentDirectory = Path.GetDirectoryName(walker);
                if (string.IsNullOrEmpty(parentDirectory))
                {
                    _ThrowInsufficientDepth(level, containingDirectoryPath);
                }

                walker = parentDirectory;
            }

            var segment = Path.GetFileName(walker);
            if (string.IsNullOrEmpty(segment))
            {
                _ThrowInsufficientDepth(level, containingDirectoryPath);
            }

            return segment;
        }

        /// <summary>
        /// Replaces one ancestor folder segment and returns the rewritten absolute directory path.
        /// </summary>
        /// <param name="containingDirectoryPath">Absolute directory that holds the rename item.</param>
        /// <param name="level">Same meaning as <see cref="GetSegmentName"/>.</param>
        /// <param name="newSegmentName">New folder segment without separators; must satisfy filename rules.</param>
        /// <returns>The new containing-directory path.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is less than <c>1</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="newSegmentName"/> is not a valid folder segment.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the path is too shallow for <paramref name="level"/>.</exception>
        public static string ReplaceSegment(string containingDirectoryPath, int level, string newSegmentName)
        {
            _RequireLevel(level);
            var validatedSegmentName = _RequireValidNewSegmentName(newSegmentName);

            var deeperBelowReplaced = new Stack<string>();
            var walker = Path.TrimEndingDirectorySeparator(containingDirectoryPath);

            for (var upwardStep = 1; upwardStep < level; upwardStep++)
            {
                var name = Path.GetFileName(walker);
                if (string.IsNullOrEmpty(name))
                {
                    _ThrowInsufficientDepth(level, containingDirectoryPath);
                }

                deeperBelowReplaced.Push(name);
                var parentDirectory = Path.GetDirectoryName(walker);
                if (string.IsNullOrEmpty(parentDirectory))
                {
                    _ThrowInsufficientDepth(level, containingDirectoryPath);
                }

                walker = parentDirectory;
            }

            var directoryAboveReplace = Path.GetDirectoryName(walker);
            if (string.IsNullOrEmpty(directoryAboveReplace))
            {
                _ThrowInsufficientDepth(level, containingDirectoryPath);
            }

            var rebuilt = Path.Combine(directoryAboveReplace, validatedSegmentName);
            while (deeperBelowReplaced.Count > 0)
            {
                rebuilt = Path.Combine(rebuilt, deeperBelowReplaced.Pop());
            }

            return rebuilt;
        }

        private static void _RequireLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(level),
                    actualValue: level,
                    message: "Ancestor folder level must be at least 1.");
            }
        }

        [DoesNotReturn]
        private static void _ThrowInsufficientDepth(int level, string containingDirectoryPath)
        {
            throw new InvalidOperationException(
                $"Cannot resolve ancestor folder at level {level}: directory path has insufficient depth ('{containingDirectoryPath}').");
        }

        private static string _RequireValidNewSegmentName(string newSegmentName)
        {
            if (string.IsNullOrWhiteSpace(newSegmentName))
            {
                throw new ArgumentException(
                    "Ancestor folder segment name cannot be empty or whitespace.",
                    nameof(newSegmentName));
            }

            if (newSegmentName != newSegmentName.Trim())
            {
                throw new ArgumentException(
                    "Ancestor folder segment name cannot have leading or trailing whitespace.",
                    nameof(newSegmentName));
            }

            if (newSegmentName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException(
                    $"Ancestor folder segment '{newSegmentName}' contains invalid characters for this platform.",
                    nameof(newSegmentName));
            }

            return newSegmentName;
        }
    }
}
