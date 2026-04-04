using Mfr.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

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

        /// <summary>
        /// Gets the resolved file items in insertion/discovery order.
        /// </summary>
        public IReadOnlyList<FileEntryLite> RenameItems => _resolvedItems;

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
            return _AppendPaths(resolvedPaths);
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
                // Directory sources resolve to the directory path itself.
                return [fullSource];
            }

            // Non-directory sources resolve relative to their parent directory.
            var parentDirectory = Path.GetDirectoryName(fullSource);
            parentDirectory = string.IsNullOrWhiteSpace(parentDirectory) ? Directory.GetCurrentDirectory() : parentDirectory;
            if (_TryResolveGlob(fullSource, out var globMatches))
            {
                return globMatches;
            }

            if (!Directory.Exists(parentDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{parentDirectory}'.");
            }

            var filePattern = Path.GetFileName(fullSource);
            if (_ContainsGlobPattern(filePattern))
            {
                // Simple file-name wildcards expand in the parent directory only.
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
        /// Tries to resolve glob sources using standard matcher syntax.
        /// </summary>
        /// <param name="fullSource">The full source path to inspect.</param>
        /// <param name="resolvedPaths">Resolved file paths when glob syntax is recognized.</param>
        /// <returns><c>true</c> if glob syntax was detected; otherwise <c>false</c>.</returns>
        private static bool _TryResolveGlob(string fullSource, out IEnumerable<string> resolvedPaths)
        {
            resolvedPaths = [];
            if (!_ContainsGlobPattern(fullSource))
            {
                return false;
            }

            var normalizedSource = fullSource.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var root = Path.GetPathRoot(normalizedSource) ?? Directory.GetCurrentDirectory();
            var relativePath = normalizedSource[root.Length..];
            var segments = relativePath.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);

            var baseSegments = new List<string>();
            var globSegments = new List<string>();
            var foundGlob = false;
            foreach (var segment in segments)
            {
                if (!foundGlob && !_ContainsGlobPattern(segment))
                {
                    baseSegments.Add(segment);
                    continue;
                }

                foundGlob = true;
                globSegments.Add(segment);
            }

            if (!foundGlob)
            {
                return false;
            }

            if (globSegments.Count == 1)
            {
                // Single-segment patterns are handled by Directory.EnumerateFiles in the caller.
                return false;
            }

            var baseDirectory = root;
            foreach (var segment in baseSegments)
            {
                baseDirectory = Path.Combine(baseDirectory, segment);
            }

            if (!Directory.Exists(baseDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{baseDirectory}'.");
            }

            var includePattern = globSegments.Count == 0 ? "*" : string.Join('/', globSegments);
            var matcher = new Matcher(
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
            _ = matcher.AddInclude(includePattern);
            var matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory)));

            resolvedPaths = matchResult.Files
                .Select(match => Path.GetFullPath(
                    Path.Combine(baseDirectory, match.Path.Replace('/', Path.DirectorySeparatorChar))));
            return true;
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

        /// <summary>
        /// Appends resolved paths to <see cref="RenameItems"/> while enforcing deduplication and filtering.
        /// </summary>
        /// <param name="resolvedPaths">Resolved file paths to append.</param>
        /// <returns>The count of newly added resolved items.</returns>
        private int _AppendPaths(IEnumerable<string> resolvedPaths)
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
                    continue;
                }

                var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
                var prefix = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath);
                var inFolderIndex = _folderCounts.GetValueOrDefault(directoryPath);
                _folderCounts[directoryPath] = inFolderIndex + 1;

                _resolvedItems.Add(new FileEntryLite(
                    GlobalIndex: _resolvedItems.Count,
                    InFolderIndex: inFolderIndex,
                    FullPath: fullPath,
                    DirectoryPath: directoryPath,
                    Prefix: prefix,
                    Extension: extension));
                addedCount++;
            }

            return addedCount;
        }
    }
}
