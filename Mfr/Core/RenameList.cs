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

        private readonly HashSet<string> _resolvedPathToIsIncluded = new(_pathComparer);
        private readonly List<RenameItem> _renameItems = [];
        private readonly Dictionary<string, int> _folderPathToCount = new(_pathComparer);
        private readonly bool _includeHidden = includeHidden;

        /// <summary>
        /// Gets the resolved file items in insertion/discovery order.
        /// </summary>
        public IReadOnlyList<RenameItem> RenameItems => _renameItems;

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
        /// Previews rename outcomes for the current list without touching the filesystem.
        /// </summary>
        /// <param name="preset">The rename preset (sequence of enabled filters).</param>
        public void Preview(FilterPreset preset)
        {
            foreach (var item in _renameItems)
            {
                item.ResetState();
            }

            foreach (var renameItem in _renameItems)
            {
                try
                {
                    renameItem.ApplyFilters(preset.Filters);
                    if (!renameItem.HasPreview())
                    {
                        throw new InvalidOperationException("Preview not generated");
                    }

                    renameItem.Status = RenameStatus.PreviewOk;
                }
                catch (Exception ex)
                {
                    renameItem.SetPreviewError(message: ex.Message, cause: ex);
                }
            }

            _MarkPreviewConflicts();
        }

        /// <summary>
        /// Commits previously previewed rename operations.
        /// </summary>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <returns>Per-item commit outcomes including success, skipped, and errors.</returns>
        public IReadOnlyList<RenameResultItem> Commit(bool failFast)
        {
            var results = new List<RenameResultItem>(_renameItems.Count);
            var stopped = false;

            foreach (var item in _renameItems)
            {
                item.CommitError = null;
                var sourcePath = item.Original.FullPath;

                if (stopped || !item.HasPreview() || item.IsPreviewPathSameAsOriginal())
                {
                    item.Status = RenameStatus.CommitSkipped;
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: item.Preview?.FullPath ?? sourcePath,
                        Status: RenameStatus.CommitSkipped,
                        Error: null));
                    continue;
                }

                var destPath = item.Preview!.FullPath;
                try
                {
                    item.Commit();
                    item.Status = RenameStatus.CommitOk;
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitOk,
                        Error: null));
                }
                catch (Exception ex)
                {
                    item.CommitError = new RenameItemError(Message: ex.Message, Cause: ex);
                    item.Status = RenameStatus.CommitError;
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitError,
                        Error: item.CommitError.Message));
                    stopped = failFast;
                }
            }

            foreach (var item in _renameItems)
            {
                item.ClearPreview();
            }

            return results;
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
        /// Marks preview conflicts (duplicate destinations or existing destination paths) as preview errors.
        /// </summary>
        private void _MarkPreviewConflicts()
        {
            var candidateItems = _renameItems
                .Where(item => item.HasPreview() && item.Status == RenameStatus.PreviewOk)
                .ToList();
            var sourcePathToIsMoving = candidateItems
                .Select(item => item.Original.FullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var destinationPathToIsDuplicate = candidateItems
                .Select(item => item.Preview!.FullPath)
                .GroupBy(destinationPath => destinationPath, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in candidateItems)
            {
                var destinationPath = item.Preview!.FullPath;
                var destinationPathIsDuplicate = destinationPathToIsDuplicate.Contains(destinationPath);
                if (destinationPathIsDuplicate)
                {
                    item.SetPreviewError(
                        message: $"Preview conflict for destination '{destinationPath}'.",
                        cause: null);
                    continue;
                }

                var destinationExistsOutsideBatch = File.Exists(destinationPath)
                    && !sourcePathToIsMoving.Contains(destinationPath);
                if (destinationExistsOutsideBatch)
                {
                    item.SetPreviewError(
                        message: $"Preview conflict for destination '{destinationPath}'.",
                        cause: null);
                    continue;
                }
            }
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
                if (!_resolvedPathToIsIncluded.Add(normalizedResolvedPath))
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
                var inFolderIndex = _folderPathToCount.GetValueOrDefault(directoryPath);
                _folderPathToCount[directoryPath] = inFolderIndex + 1;

                var globalIndex = _renameItems.Count;
                var fileMeta = new FileMeta(
                    globalIndex: globalIndex,
                    inFolderIndex: inFolderIndex,
                    directoryPath: directoryPath,
                    prefix: prefix,
                    extension: extension);
                var renameItem = new RenameItem(fileMeta);
                _renameItems.Add(renameItem);
                addedCount++;
            }

            return addedCount;
        }
    }
}
