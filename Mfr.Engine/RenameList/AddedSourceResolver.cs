using Mfr.Utils;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Resolves user-added rename sources (files, directories, and last-segment filename masks) into concrete paths.
    /// </summary>
    internal static class AddedSourceResolver
    {
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
                var parentDirectory = _RequireExistingDirectory(Path.GetDirectoryName(fullSource));
                return _ResolveDirectory(
                    directoryPath: parentDirectory,
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
                    directoryPath: fullSource,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    includeSubdirs: includeSubdirs,
                    includeMask: null,
                    excludeMasks: excludeMasks
                );
            }

            _RequireExistingDirectory(Path.GetDirectoryName(fullSource));
            if (File.Exists(fullSource))
            {
                // Exact file sources resolve to that single file and bypass include/exclude masks.
                return [fullSource];
            }

            // Missing exact files are ignored (no resolved items).
            return [];
        }

        /// <summary>
        /// Resolves a directory source: the directory itself when folders are requested, then matching files
        /// and (when recursing) matching descendant folders.
        /// </summary>
        private static List<string> _ResolveDirectory(
            string directoryPath,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            _ThrowIfRootPath(directoryPath);

            var results = new List<string>();
            if (includeFolders)
            {
                // The explicit directory source is always included when folders are requested.
                results.Add(directoryPath);
            }

            var searchOption = includeSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (includeFiles)
            {
                var filePaths = Directory.EnumerateFiles(directoryPath, "*", searchOption);
                foreach (var filePath in filePaths)
                {
                    _AddIfNameMatches(
                        fullPath: filePath,
                        results: results,
                        includeMask: includeMask,
                        excludeMasks: excludeMasks
                    );
                }
            }

            if (includeFolders && includeSubdirs)
            {
                var folderPaths = Directory.EnumerateDirectories(
                    directoryPath,
                    "*",
                    SearchOption.AllDirectories
                );
                foreach (var folderPath in folderPaths)
                {
                    _AddIfNameMatches(
                        fullPath: folderPath,
                        results: results,
                        includeMask: includeMask,
                        excludeMasks: excludeMasks
                    );
                }
            }

            return results;
        }

        /// <summary>
        /// Adds a discovered path when its file name passes include and exclude masks.
        /// </summary>
        private static void _AddIfNameMatches(
            string fullPath,
            List<string> results,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks
        )
        {
            var fileName = Path.GetFileName(fullPath);
            if (!WildcardMask.IsMatch(fileName, includeMask))
            {
                return;
            }

            if (WildcardMask.MatchesAny(fileName, excludeMasks))
            {
                return;
            }

            results.Add(fullPath);
        }

        /// <summary>
        /// Rejects <c>**</c> and wildcards in any path segment except the last.
        /// </summary>
        private static void _EnsureWildcardOnlyInLastSegment(string fullSource)
        {
            if (fullSource.Contains("**", StringComparison.Ordinal))
            {
                throw new UserException(
                    "Recursive '**' globs are not supported. Enable recursive directory expansion to include subdirectories."
                );
            }

            var normalized = fullSource.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var lastSeparator = normalized.LastIndexOf(Path.DirectorySeparatorChar);
            var prefix = lastSeparator >= 0 ? normalized[..lastSeparator] : string.Empty;
            if (_ContainsGlobPattern(prefix))
            {
                throw new UserException(
                    "Wildcards are only allowed in the last path segment. Enable recursive directory expansion to include subdirectories."
                );
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
        /// Returns <paramref name="directoryPath"/> when it exists; otherwise throws.
        /// </summary>
        private static string _RequireExistingDirectory(string? directoryPath)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
            {
                return directoryPath;
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
