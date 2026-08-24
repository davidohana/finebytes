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
        /// <param name="cancellationToken">When canceled, stops enumeration and returns without throwing.</param>
        /// <returns>Resolved paths for the source.</returns>
        internal static IEnumerable<string> ResolveToPaths(
            string source,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            IReadOnlyList<string>? excludeMasks = null,
            CancellationToken cancellationToken = default
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
                    excludeMasks: excludeMasks,
                    cancellationToken: cancellationToken
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
                    excludeMasks: excludeMasks,
                    cancellationToken: cancellationToken
                );
            }

            _RequireExistingDirectory(Path.GetDirectoryName(fullSource));
            if (File.Exists(fullSource))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return [];
                }

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
        private static IEnumerable<string> _ResolveDirectory(
            string directoryPath,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks,
            CancellationToken cancellationToken
        )
        {
            _ThrowIfRootPath(directoryPath);
            _EnsureDirectoryReadable(directoryPath);

            if (includeFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                // The explicit directory source is always included when folders are requested.
                yield return directoryPath;
            }

            // Skip entries we cannot open instead of failing the whole add (File List does the same).
            var enumerationOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = includeSubdirs,
                ReturnSpecialDirectories = false,
            };

            if (includeFiles)
            {
                var matchingFiles = _YieldMatching(
                    Directory.EnumerateFiles(directoryPath, "*", enumerationOptions),
                    includeMask,
                    excludeMasks,
                    cancellationToken
                );
                foreach (var filePath in matchingFiles)
                {
                    yield return filePath;
                }
            }

            if (!includeFolders || !includeSubdirs)
            {
                yield break;
            }

            var matchingFolders = _YieldMatching(
                Directory.EnumerateDirectories(directoryPath, "*", enumerationOptions),
                includeMask,
                excludeMasks,
                cancellationToken
            );
            foreach (var folderPath in matchingFolders)
            {
                yield return folderPath;
            }
        }

        /// <summary>
        /// Yields paths whose file names pass include and exclude masks until canceled.
        /// </summary>
        private static IEnumerable<string> _YieldMatching(
            IEnumerable<string> paths,
            string? includeMask,
            IReadOnlyList<string>? excludeMasks,
            CancellationToken cancellationToken
        )
        {
            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (_NameMatches(path, includeMask, excludeMasks))
                {
                    yield return path;
                }
            }
        }

        /// <summary>
        /// Returns whether a discovered path's file name passes include and exclude masks.
        /// </summary>
        private static bool _NameMatches(string fullPath, string? includeMask, IReadOnlyList<string>? excludeMasks)
        {
            var fileName = Path.GetFileName(fullPath);
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
        /// Throws when <paramref name="directoryPath"/> cannot be listed (for example access denied).
        /// </summary>
        /// <para>
        /// Uses <see cref="EnumerationOptions.IgnoreInaccessible"/> = false so denial on the directory itself
        /// surfaces as an exception instead of an empty resolution.
        /// </para>
        private static void _EnsureDirectoryReadable(string directoryPath)
        {
            try
            {
                var probeOptions = new EnumerationOptions
                {
                    IgnoreInaccessible = false,
                    RecurseSubdirectories = false,
                    ReturnSpecialDirectories = false,
                };
                _ = Directory.EnumerateFileSystemEntries(directoryPath, "*", probeOptions).Any();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UserException($"Access denied reading folder: '{directoryPath}'.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new UserException($"Directory for source does not exist: '{directoryPath}'.", ex);
            }
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
