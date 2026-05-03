using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Mutation, filter-target read/write dispatch, and path helpers for a <see cref="FileMeta"/> rename snapshot.
    /// </summary>
    internal static class FileMetaPreviewExtensions
    {
        /// <summary>
        /// Returns the string value addressed by <paramref name="target"/> on this snapshot.
        /// </summary>
        /// <param name="meta">Metadata snapshot.</param>
        /// <param name="target">The filter target.</param>
        /// <returns>The current value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an ancestor-folder level argument is invalid; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an ancestor-folder segment cannot be resolved; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static string GetTargetString(this FileMeta meta, FilterTarget target)
        {
            return target switch
            {
                FilePrefixTarget => meta.Prefix,
                FileExtensionTarget => meta.Extension,
                FileFullNameTarget => meta.Prefix + meta.Extension,
                FullPathTarget => meta.FullPath,
                ParentDirectoryTarget => meta.DirectoryPath,
                AncestorFolderTarget ancestorFolderTarget =>
                    meta.GetAncestorFolderSegmentName(ancestorFolderTarget.Level),
                _ => throw new NotSupportedException($"Unsupported filter target '{target.GetType().Name}'.")
            };
        }

        /// <summary>
        /// Writes the transformed string for <paramref name="target"/> onto this snapshot.
        /// </summary>
        /// <param name="meta">Metadata snapshot.</param>
        /// <param name="target">The filter target.</param>
        /// <param name="value">The transformed value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an ancestor-folder level argument is invalid; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> cannot be assigned for the addressed target (ancestor-folder constraints or invalid paths).
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when an ancestor-folder segment cannot be resolved; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static void SetTargetString(this FileMeta meta, FilterTarget target, string value)
        {
            switch (target)
            {
                case FilePrefixTarget:
                    meta.Prefix = value;
                    return;
                case FileExtensionTarget:
                    meta.Extension = value;
                    return;
                case FileFullNameTarget:
                    _SetFullFileNameFromValue(meta, value);
                    return;
                case FullPathTarget:
                    meta.SetFromAbsoluteFullPath(value);
                    return;
                case ParentDirectoryTarget:
                    meta.SetAbsoluteDirectoryPath(value);
                    return;
                case AncestorFolderTarget ancestorFolderTarget:
                    meta.ReplaceAncestorFolderSegment(ancestorFolderTarget.Level, value);
                    return;
                default:
                    throw new NotSupportedException($"Unsupported filter target '{target.GetType().Name}'.");
            }
        }

        private static void _SetFullFileNameFromValue(FileMeta meta, string fullValue)
        {
            var fullName = Path.GetFileName(fullValue);
            meta.Extension = Path.GetExtension(fullName);
            meta.Prefix = Path.GetFileNameWithoutExtension(fullName);
        }

        /// <summary>
        /// Returns one ancestor folder segment name counted from this directory path toward the volume root.
        /// </summary>
        /// <param name="meta">Metadata snapshot.</param>
        /// <param name="level">Ancestor level (<c>1</c>-based).</param>
        internal static string GetAncestorFolderSegmentName(this FileMeta meta, int level)
        {
            return DirectoryPathAncestor.GetSegmentName(
                containingDirectoryPath: meta.DirectoryPath,
                level: level);
        }

        /// <summary>
        /// Replaces directory path, prefix, and extension from a fully qualified file path string.
        /// </summary>
        /// <param name="meta">Metadata snapshot to mutate.</param>
        /// <param name="value">New absolute path including file name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is whitespace-only, not fully qualified, missing a directory,
        /// missing a file name, invalid for use as a path, or resolves to directory name <c>null</c>.
        /// </exception>
        internal static void SetFromAbsoluteFullPath(this FileMeta meta, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Full path cannot be empty or whitespace.", nameof(value));
            }

            if (!Path.IsPathFullyQualified(trimmed))
            {
                throw new ArgumentException("Full path must be fully qualified.", nameof(value));
            }

            if (_ContainsInvalidPathChar(trimmed))
            {
                throw new ArgumentException("Full path contains invalid characters.", nameof(value));
            }

            var fileName = Path.GetFileName(trimmed);
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Full path must include a file name.", nameof(value));
            }

            var directory = Path.GetDirectoryName(trimmed);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Full path must include a parent directory.", nameof(value));
            }

            meta.DirectoryPath = directory;
            meta.Extension = Path.GetExtension(fileName);
            meta.Prefix = Path.GetFileNameWithoutExtension(fileName);
        }

        /// <summary>
        /// Replaces the parent directory path with a normalized fully qualified directory string.
        /// </summary>
        /// <param name="meta">Metadata snapshot to mutate.</param>
        /// <param name="value">New absolute directory path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is whitespace-only, not fully qualified,
        /// or contains characters invalid for a path.
        /// </exception>
        internal static void SetAbsoluteDirectoryPath(this FileMeta meta, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Directory path cannot be empty or whitespace.", nameof(value));
            }

            if (!Path.IsPathFullyQualified(trimmed))
            {
                throw new ArgumentException("Directory path must be fully qualified.", nameof(value));
            }

            if (_ContainsInvalidPathChar(trimmed))
            {
                throw new ArgumentException("Directory path contains invalid characters.", nameof(value));
            }

            meta.DirectoryPath = trimmed;
        }

        /// <summary>
        /// Replaces one ancestor folder-name segment relative to the containing-directory path toward the volume root.
        /// </summary>
        /// <param name="meta">Metadata snapshot to mutate.</param>
        /// <param name="level">Ancestor level (<see cref="AncestorFolderTarget"/>).</param>
        /// <param name="newSegmentName">New folder segment name without path separators.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is invalid; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="newSegmentName"/> is not a valid segment; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the path is too shallow for <paramref name="level"/>.</exception>
        internal static void ReplaceAncestorFolderSegment(this FileMeta meta, int level, string newSegmentName)
        {
            meta.DirectoryPath = DirectoryPathAncestor.ReplaceSegment(
                containingDirectoryPath: meta.DirectoryPath,
                level: level,
                newSegmentName: newSegmentName);
        }

        private static bool _ContainsInvalidPathChar(string path)
        {
            foreach (var c in Path.GetInvalidPathChars())
            {
                if (path.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
