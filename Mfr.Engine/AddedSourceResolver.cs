using Mfr.Utils;

namespace Mfr.Engine
{
    /// <summary>
    /// Resolves user-added rename sources (files, directories, and last-segment filename masks) into concrete paths.
    /// </summary>
    internal static class AddedSourceResolver
    {
        private enum FolderAddStyle
        {
            FilesOnlyTop,
            FilesOnlyRecursive,
            OneLevelRecursion,
            FullRecursion,
        }

        /// <summary>
        /// Resolves a single source into file and folder paths.
        /// </summary>
        /// <param name="source">A file, a directory, or a directory plus a filename mask in the last segment.</param>
        /// <param name="includeFiles">Whether discovered file entries should be included.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="includeSubdirs">Whether directory expansion should recurse into subdirectories.</param>
        /// <param name="excludeMasks">Exclusive file-name masks for discovered entries.</param>
        /// <returns>Resolved paths for the source.</returns>
        internal static IEnumerable<string> ResolveToPaths(
            string source,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            IReadOnlyList<string>? excludeMasks = null
        )
        {
            var fullSource = Path.GetFullPath(source);
            _EnsureWildcardOnlyInLastSegment(fullSource);

            var lastSegment = Path.GetFileName(
                fullSource.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            );
            if (_ContainsGlobPattern(lastSegment))
            {
                var parentDirectory = Path.GetDirectoryName(fullSource);
                if (string.IsNullOrWhiteSpace(parentDirectory))
                {
                    parentDirectory = Directory.GetCurrentDirectory();
                }

                _ThrowIfDirectoryMissing(parentDirectory);
                _ThrowIfRootPath(parentDirectory);
                return _ResolveDirectory(
                    fullSource: parentDirectory,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    includeSubdirs: includeSubdirs,
                    includeMask: lastSegment,
                    excludeMasks: excludeMasks
                );
            }

            if (Directory.Exists(fullSource))
            {
                return _ResolveDirectory(
                    fullSource: fullSource,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    includeSubdirs: includeSubdirs,
                    includeMask: null,
                    excludeMasks: excludeMasks
                );
            }

            var parentOfExact = Path.GetDirectoryName(fullSource);
            parentOfExact = string.IsNullOrWhiteSpace(parentOfExact) ? Directory.GetCurrentDirectory() : parentOfExact;
            if (!Directory.Exists(parentOfExact))
            {
                throw new UserException($"Directory for source does not exist: '{parentOfExact}'.");
            }

            if (File.Exists(fullSource))
            {
                // Exact file sources resolve to that single file and bypass include/exclude masks.
                return [fullSource];
            }

            // Missing exact files are ignored (no resolved items).
            return [];
        }

        /// <summary>
        /// Resolves a directory source using MFR7 Adder-style expansion rules.
        /// </summary>
        private static List<string> _ResolveDirectory(
            string fullSource,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            var results = new List<string>();

            if (includeFolders)
            {
                // The explicit directory source is always included when folders are requested.
                results.Add(fullSource);
            }

            if (!includeFiles)
            {
                return results;
            }

            var folderAddStyle = _ResolveFolderAddStyle(includeFolders: includeFolders, includeSubdirs: includeSubdirs);
            _ExpandDirectory(
                directoryPath: fullSource,
                results: results,
                folderAddStyle: folderAddStyle,
                includeMask: includeMask,
                excludeMasks: excludeMasks
            );
            return results;
        }

        /// <summary>
        /// Maps add-policy flags to the directory expansion mode used while walking a source folder.
        /// </summary>
        private static FolderAddStyle _ResolveFolderAddStyle(bool includeFolders, bool includeSubdirs)
        {
            if (!includeFolders)
            {
                return includeSubdirs ? FolderAddStyle.FilesOnlyRecursive : FolderAddStyle.FilesOnlyTop;
            }

            return includeSubdirs ? FolderAddStyle.FullRecursion : FolderAddStyle.OneLevelRecursion;
        }

        /// <summary>
        /// Expands directory contents depth-first, applying masks to discovered file and folder names.
        /// </summary>
        private static void _ExpandDirectory(
            string directoryPath,
            List<string> results,
            FolderAddStyle folderAddStyle,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            if (folderAddStyle == FolderAddStyle.FilesOnlyTop)
            {
                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    _TryAddDiscoveredPath(
                        fullPath: filePath,
                        results: results,
                        includeMask: includeMask,
                        excludeMasks: excludeMasks
                    );
                }

                return;
            }

            if (folderAddStyle == FolderAddStyle.FilesOnlyRecursive)
            {
                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    _TryAddDiscoveredPath(
                        fullPath: filePath,
                        results: results,
                        includeMask: includeMask,
                        excludeMasks: excludeMasks
                    );
                }

                return;
            }

            foreach (var entry in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos())
            {
                var isDirectory = entry.Attributes.HasFlag(FileAttributes.Directory);
                _AddDiscoveredEntry(
                    fullPath: entry.FullName,
                    isDirectory: isDirectory,
                    results: results,
                    folderAddStyle: folderAddStyle,
                    includeMask: includeMask,
                    excludeMasks: excludeMasks
                );
            }
        }

        /// <summary>
        /// Adds a discovered child entry and optionally recurses into nested folders.
        /// </summary>
        private static void _AddDiscoveredEntry(
            string fullPath,
            bool isDirectory,
            List<string> results,
            FolderAddStyle folderAddStyle,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            if (
                !_TryAddDiscoveredPath(
                    fullPath: fullPath,
                    results: results,
                    includeMask: includeMask,
                    excludeMasks: excludeMasks
                )
            )
            {
                return;
            }

            if (!isDirectory)
            {
                return;
            }

            if (folderAddStyle == FolderAddStyle.OneLevelRecursion)
            {
                return;
            }

            foreach (var entry in new DirectoryInfo(fullPath).EnumerateFileSystemInfos())
            {
                var isNestedDirectory = entry.Attributes.HasFlag(FileAttributes.Directory);
                _AddDiscoveredEntry(
                    fullPath: entry.FullName,
                    isDirectory: isNestedDirectory,
                    results: results,
                    folderAddStyle: folderAddStyle,
                    includeMask: includeMask,
                    excludeMasks: excludeMasks
                );
            }
        }

        /// <summary>
        /// Adds a discovered path when its file name passes include and exclude masks.
        /// </summary>
        /// <returns><c>true</c> when the path was added; otherwise <c>false</c>.</returns>
        private static bool _TryAddDiscoveredPath(
            string fullPath,
            List<string> results,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            var fileName = Path.GetFileName(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            );
            if (!_PassesFileNameMasks(fileName: fileName, includeMask: includeMask, excludeMasks: excludeMasks))
            {
                return false;
            }

            results.Add(fullPath);
            return true;
        }

        /// <summary>
        /// Whether a discovered file or folder name passes include and exclude masks.
        /// </summary>
        private static bool _PassesFileNameMasks(
            string fileName,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            if (!WildcardMask.IsMatch(fileName, includeMask))
            {
                return false;
            }

            if (WildcardMask.MatchesAny(fileName, excludeMasks))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Rejects <c>**</c> and wildcards in any path segment except the last.
        /// </summary>
        private static void _EnsureWildcardOnlyInLastSegment(string fullSource)
        {
            var normalized = fullSource.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var root = Path.GetPathRoot(normalized) ?? string.Empty;
            var relativePath = normalized[root.Length..];
            var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var isLast = i == segments.Length - 1;
                if (segment.Contains("**", StringComparison.Ordinal))
                {
                    throw new UserException(
                        "Recursive '**' globs are not supported. Enable recursive directory expansion to include subdirectories."
                    );
                }

                if (!isLast && _ContainsGlobPattern(segment))
                {
                    throw new UserException(
                        "Wildcards are only allowed in the last path segment. Enable recursive directory expansion to include subdirectories."
                    );
                }
            }
        }

        /// <summary>
        /// Throws when <paramref name="directoryPath"/> is a drive or filesystem root.
        /// </summary>
        private static void _ThrowIfRootPath(string directoryPath)
        {
            var fullDirectory = Path.GetFullPath(directoryPath);
            var root = Path.GetPathRoot(fullDirectory) ?? string.Empty;
            if (string.Equals(root, fullDirectory, PathComparers.OsComparison))
            {
                throw new UserException($"Root paths cannot be added as rename sources: '{directoryPath}'.");
            }
        }

        /// <summary>
        /// Throws when the directory that a last-segment mask applies to does not exist.
        /// </summary>
        private static void _ThrowIfDirectoryMissing(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                return;
            }

            throw new UserException($"Directory for source does not exist: '{directoryPath}'.");
        }

        /// <summary>
        /// Determines whether the value contains wildcard characters.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns><c>true</c> when wildcard characters are present; otherwise <c>false</c>.</returns>
        private static bool _ContainsGlobPattern(string value)
        {
            return value.Contains('*') || value.Contains('?');
        }
    }
}
