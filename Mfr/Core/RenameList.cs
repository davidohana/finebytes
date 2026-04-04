using Mfr.Models;

namespace Mfr.Core
{
    /// <summary>
    /// Maintains ordered rename sources and resolves them into file entries.
    /// </summary>
    /// <param name="includeHidden">If <c>true</c>, includes hidden/system files while resolving.</param>
    public sealed class RenameList(bool includeHidden)
    {
        private static readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        private readonly HashSet<string> _resolvedPathKeys = new(_pathComparer);
        private readonly List<FileEntryLite> _resolvedItems = [];
        private readonly Dictionary<string, int> _folderCounts = new(_pathComparer);
        private readonly bool _includeHidden = includeHidden;
        private int _nextGlobalIndex;

        /// <summary>
        /// Gets the resolved file items in insertion/discovery order.
        /// </summary>
        public IReadOnlyList<FileEntryLite> ResolvedItems => _resolvedItems;

        /// <summary>
        /// Adds and resolves a single source.
        /// </summary>
        /// <param name="source">A file path, directory path, or wildcard source.</param>
        /// <returns>The count of newly added resolved items.</returns>
        public int AddSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new UserException("Source cannot be empty.");
            }

            var trimmedSource = source.Trim();
            var resolvedPaths = _ResolveSource(trimmedSource).ToList();
            return _ResolveAndAppendPaths(resolvedPaths);
        }

        /// <summary>
        /// Adds multiple sources while preserving insertion order.
        /// </summary>
        /// <param name="sources">Sources to add.</param>
        public void AddSources(IEnumerable<string> sources)
        {
            foreach (var source in sources)
            {
                _ = AddSource(source);
            }
        }

        /// <summary>
        /// Normalizes a path into a platform-consistent comparison key.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path key.</returns>
        private static string _NormalizePathKey(string path)
        {
            var normalized = Path.GetFullPath(path);
            return OperatingSystem.IsWindows() ? normalized.Replace('/', '\\') : normalized;
        }

        /// <summary>
        /// Resolves a single source into file paths.
        /// </summary>
        /// <param name="source">The source to resolve.</param>
        /// <returns>Resolved file paths for the source.</returns>
        private static IEnumerable<string> _ResolveSource(string source)
        {
            var fullSource = Path.GetFullPath(source);
            if (Directory.Exists(fullSource))
            {
                // Directory sources expand to top-level files only.
                return Directory.EnumerateFiles(fullSource, "*", SearchOption.TopDirectoryOnly);
            }

            // Non-directory sources resolve relative to their parent directory.
            var parentDirectory = Path.GetDirectoryName(fullSource);
            parentDirectory = string.IsNullOrWhiteSpace(parentDirectory) ? Directory.GetCurrentDirectory() : parentDirectory;
            if (_TryResolveRecursiveGlob(fullSource, out var recursiveMatches))
            {
                return recursiveMatches;
            }

            if (!Directory.Exists(parentDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{parentDirectory}'.");
            }

            var filePattern = Path.GetFileName(fullSource);
            if (_ContainsWildcard(filePattern))
            {
                // Wildcard sources expand inside the parent directory.
                return Directory.EnumerateFiles(parentDirectory, filePattern, SearchOption.TopDirectoryOnly);
            }

            if (File.Exists(fullSource))
            {
                // Exact file source resolves to that single file.
                return [fullSource];
            }

            // Missing exact files are ignored (no resolved items).
            return [];
        }

        /// <summary>
        /// Tries to resolve recursive glob sources that use <c>**</c> syntax.
        /// </summary>
        /// <param name="fullSource">The full source path to inspect.</param>
        /// <param name="resolvedPaths">Resolved file paths when recursive syntax is recognized.</param>
        /// <returns><c>true</c> if recursive glob syntax was detected; otherwise <c>false</c>.</returns>
        private static bool _TryResolveRecursiveGlob(string fullSource, out IEnumerable<string> resolvedPaths)
        {
            resolvedPaths = [];
            var normalizedSource = fullSource.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var recursiveSegment = $"{Path.DirectorySeparatorChar}**{Path.DirectorySeparatorChar}";
            var recursiveSegmentIndex = normalizedSource.IndexOf(recursiveSegment, StringComparison.Ordinal);

            string rootDirectory;
            string searchPattern;
            if (recursiveSegmentIndex >= 0)
            {
                rootDirectory = normalizedSource[..recursiveSegmentIndex];
                searchPattern = normalizedSource[(recursiveSegmentIndex + recursiveSegment.Length)..];
            }
            else
            {
                var trailingRecursiveSegment = $"{Path.DirectorySeparatorChar}**";
                if (!normalizedSource.EndsWith(trailingRecursiveSegment, StringComparison.Ordinal))
                {
                    return false;
                }

                rootDirectory = normalizedSource[..^trailingRecursiveSegment.Length];
                searchPattern = "*";
            }

            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                rootDirectory = Path.GetPathRoot(normalizedSource) ?? Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(rootDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{rootDirectory}'.");
            }

            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*";
            }

            if (searchPattern.Contains(Path.DirectorySeparatorChar))
            {
                throw new UserException($"Recursive glob source '{fullSource}' supports file-name patterns only after '**'.");
            }

            resolvedPaths = Directory.EnumerateFiles(rootDirectory, searchPattern, SearchOption.AllDirectories);
            return true;
        }

        /// <summary>
        /// Determines whether the value contains wildcard characters.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns><c>true</c> when wildcard characters are present; otherwise <c>false</c>.</returns>
        private static bool _ContainsWildcard(string value)
        {
            return value.Contains('*') || value.Contains('?');
        }

        /// <summary>
        /// Appends resolved paths to <see cref="ResolvedItems"/> while enforcing deduplication and filtering.
        /// </summary>
        /// <param name="resolvedPaths">Resolved file paths to append.</param>
        /// <returns>The count of newly added resolved items.</returns>
        private int _ResolveAndAppendPaths(IEnumerable<string> resolvedPaths)
        {
            var addedCount = 0;
            foreach (var fullPath in resolvedPaths)
            {
                var normalizedResolvedPath = _NormalizePathKey(fullPath);
                if (!_resolvedPathKeys.Add(normalizedResolvedPath))
                {
                    continue;
                }

                var attrs = File.GetAttributes(fullPath);
                if (!_includeHidden &&
                    (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System)))
                {
                    _nextGlobalIndex++;
                    continue;
                }

                var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
                var prefix = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath);
                var folderOccurrence = _folderCounts.GetValueOrDefault(directoryPath);
                _folderCounts[directoryPath] = folderOccurrence + 1;

                _resolvedItems.Add(new FileEntryLite(
                    GlobalIndex: _nextGlobalIndex,
                    FolderOccurrenceIndex: folderOccurrence,
                    FullPath: fullPath,
                    DirectoryPath: directoryPath,
                    Prefix: prefix,
                    Extension: extension));
                addedCount++;
                _nextGlobalIndex++;
            }

            return addedCount;
        }
    }
}
