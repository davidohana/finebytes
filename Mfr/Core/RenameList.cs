using System.Text;
using Mfr.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Serilog;

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
        /// Adds multiple sources while preserving insertion order.
        /// </summary>
        /// <param name="sources">Sources to add.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="recursiveDirectoryFileAdd">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        public void AddSources(
            IEnumerable<string> sources,
            bool includeFiles = true,
            bool includeFolders = true,
            bool recursiveDirectoryFileAdd = false)
        {
            var sourceList = sources.ToList();
            Log.Information(
                "Received {SourceCount} source(s) for resolution. IncludeFiles: {IncludeFiles}, IncludeFolders: {IncludeFolders}, RecursiveDirectoryFileAdd: {RecursiveDirectoryFileAdd}, IncludeHidden: {IncludeHidden}.",
                sourceList.Count,
                includeFiles,
                includeFolders,
                recursiveDirectoryFileAdd,
                _includeHidden);

            foreach (var source in sourceList)
            {
                _ = AddSource(
                    source: source,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    recursiveDirectoryFileAdd: recursiveDirectoryFileAdd);
            }
        }

        /// <summary>
        /// Adds and resolves a single source.
        /// </summary>
        /// <param name="source">A file path, directory path, or wildcard source.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="recursiveDirectoryFileAdd">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        /// <returns>The count of newly added resolved items.</returns>
        public int AddSource(
            string source,
            bool includeFiles = true,
            bool includeFolders = true,
            bool recursiveDirectoryFileAdd = false)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new UserException("Source cannot be empty.");
            }

            var trimmedSource = source.Trim();
            var resolvedPaths = _ResolveSource(
                source: trimmedSource,
                includeFolders: includeFolders,
                recursiveDirectoryFileAdd: recursiveDirectoryFileAdd).ToList();
            var addedCount = _AppendPaths(
                resolvedPaths: resolvedPaths,
                includeFiles: includeFiles,
                includeFolders: includeFolders);
            Log.Information(
                "Resolved source '{Source}' to {ResolvedCount} path(s), added {AddedCount} new item(s).",
                trimmedSource,
                resolvedPaths.Count,
                addedCount);
            return addedCount;
        }

        /// <summary>
        /// Previews rename outcomes for the current list without touching the filesystem.
        /// </summary>
        /// <param name="preset">The rename preset (sequence of enabled filters).</param>
        public void Preview(FilterPreset preset)
        {
            Log.Information(
                "Starting preview for preset '{PresetName}' with {ItemCount} item(s).",
                preset.Name,
                _renameItems.Count);

            foreach (var item in _renameItems)
            {
                item.ResetState();
            }

            foreach (var renameItem in _renameItems)
            {
                try
                {
                    renameItem.ApplyFilters(preset.Filters);
                    renameItem.Status = RenameStatus.PreviewOk;
                }
                catch (Exception ex)
                {
                    renameItem.SetPreviewError(message: ex.Message, cause: ex);
                    Log.Warning(
                        ex,
                        "Preview failed for '{SourcePath}'.",
                        renameItem.Original.FullPath);
                }
            }

            _MarkPreviewConflicts();
            foreach (var renameItem in _renameItems)
            {
                _LogPreviewChangeDetail(renameItem: renameItem);
            }

            var previewChangedCount = _renameItems.Count(item => !item.IsPreviewPathSameAsOriginal());
            var previewUnchangedCount = _renameItems.Count(item => item.IsPreviewPathSameAsOriginal());
            var previewErrorCount = _renameItems.Count(item => item.Status == RenameStatus.PreviewError);
            Log.Information(
                "Finished preview. Changed: {PreviewChangedCount}, Unchanged: {PreviewUnchangedCount}, Errors: {PreviewErrorCount}.",
                previewChangedCount,
                previewUnchangedCount,
                previewErrorCount);
        }

        /// <summary>
        /// Commits previously previewed rename operations.
        /// </summary>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <param name="dryRun">If <c>true</c>, simulates commit outcomes without applying filesystem changes.</param>
        /// <returns>Per-item commit outcomes including success, skipped, and errors.</returns>
        public IReadOnlyList<RenameResultItem> Commit(bool failFast, bool dryRun = false)
        {
            Log.Information(
                "Starting commit for {ItemCount} item(s). FailFast: {FailFast}. DryRun: {DryRun}.",
                _renameItems.Count,
                failFast,
                dryRun);

            var results = new List<RenameResultItem>(_renameItems.Count);
            var stopped = false;

            foreach (var item in _renameItems)
            {
                item.CommitError = null;
                var sourcePath = item.Original.FullPath;

                if (stopped || item.IsPreviewPathSameAsOriginal())
                {
                    item.Status = RenameStatus.CommitSkipped;
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        Status: RenameStatus.CommitSkipped,
                        Error: null,
                        Changes: []));
                    continue;
                }

                var destPath = item.Preview.FullPath;
                try
                {
                    if (!dryRun)
                    {
                        item.Commit();
                    }

                    item.Status = RenameStatus.CommitOk;
                    var changes = _BuildFileNameChanges(sourcePath: sourcePath, destinationPath: destPath);
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: changes));
                }
                catch (Exception ex)
                {
                    item.CommitError = new RenameItemError(Message: ex.Message, Cause: ex);
                    item.Status = RenameStatus.CommitError;
                    Log.Error(
                        ex,
                        "Commit failed for '{SourcePath}' -> '{DestinationPath}'.",
                        sourcePath,
                        destPath);
                    results.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        Status: RenameStatus.CommitError,
                        Error: item.CommitError.Message,
                        Changes: []));
                    stopped = failFast;
                }
            }

            foreach (var item in _renameItems)
            {
                item.ClearPreview();
            }

            var commitOkCount = results.Count(item => item.Status == RenameStatus.CommitOk);
            var commitSkippedCount = results.Count(item => item.Status == RenameStatus.CommitSkipped);
            var commitErrorCount = results.Count(item => item.Status == RenameStatus.CommitError);
            Log.Information(
                "Finished commit. Success: {CommitOkCount}, Skipped: {CommitSkippedCount}, Errors: {CommitErrorCount}.",
                commitOkCount,
                commitSkippedCount,
                commitErrorCount);

            return results;
        }

        /// <summary>
        /// Builds a file-name change list for a rename result.
        /// </summary>
        /// <param name="sourcePath">Original source path.</param>
        /// <param name="destinationPath">Destination path.</param>
        /// <returns>Collection containing the file name change when the file name was modified.</returns>
        private static IReadOnlyList<RenamePropertyChange> _BuildFileNameChanges(string sourcePath, string destinationPath)
        {
            var sourceFileName = Path.GetFileName(sourcePath);
            var destinationFileName = Path.GetFileName(destinationPath);
            var changes = (IReadOnlyList<RenamePropertyChange>)[];
            var fileNameChanged = !string.Equals(sourceFileName, destinationFileName, StringComparison.OrdinalIgnoreCase);
            if (fileNameChanged)
            {
                changes =
                [
                    new RenamePropertyChange(
                        Property: "FileName",
                        OldValue: sourceFileName,
                        NewValue: destinationFileName)
                ];
            }

            return changes;
        }

        /// <summary>
        /// Logs debug details for one item when preview produced a destination path change.
        /// </summary>
        /// <param name="renameItem">The previewed item to inspect.</param>
        private static void _LogPreviewChangeDetail(RenameItem renameItem)
        {
            if (renameItem.IsPreviewPathSameAsOriginal())
            {
                return;
            }

            var originalFullPath = renameItem.Original.FullPath;
            Log.Debug("Preview changes for {OriginalFullPath}:", originalFullPath);

            var previewChanges = _BuildPreviewPropertyChanges(renameItem: renameItem);
            foreach (var change in previewChanges)
            {
                // Property on its own line; old then new below with fixed indent (avoids console-prefix alignment math).
                var changeBlock = _BuildPreviewChangeBlock(change: change);
                Log.Debug("{PreviewChangeBlock}", changeBlock);
            }

            if (renameItem.Status == RenameStatus.PreviewError)
            {
                var previewErrorMessage = renameItem.PreviewError?.Message ?? "Unknown preview error.";
                Log.Debug(
                    "  Error: '{PreviewErrorMessage}'",
                    previewErrorMessage);
            }
        }

        private static string _BuildPreviewChangeBlock(RenamePropertyChange change)
        {
            const int valueLineIndentWidth = 10;
            var valueLinePadding = new string(' ', valueLineIndentWidth);
            var builder = new StringBuilder()
                .Append("  ")
                .Append(change.Property)
                .Append(':')
                .AppendLine()
                .Append(valueLinePadding)
                .Append(change.OldValue)
                .Append(" -->")
                .AppendLine()
                .Append(valueLinePadding)
                .Append(change.NewValue);
            return builder.ToString();
        }

        private static List<RenamePropertyChange> _BuildPreviewPropertyChanges(RenameItem renameItem)
        {
            var changes = new List<RenamePropertyChange>();
            var prefixChanged = !string.Equals(renameItem.Original.Prefix, renameItem.Preview.Prefix, StringComparison.Ordinal);
            if (prefixChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Prefix",
                    OldValue: renameItem.Original.Prefix,
                    NewValue: renameItem.Preview.Prefix));
            }

            var extensionChanged = !string.Equals(renameItem.Original.Extension, renameItem.Preview.Extension, StringComparison.Ordinal);
            if (extensionChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Extension",
                    OldValue: renameItem.Original.Extension,
                    NewValue: renameItem.Preview.Extension));
            }

            var directoryChanged = !string.Equals(renameItem.Original.DirectoryPath, renameItem.Preview.DirectoryPath, StringComparison.OrdinalIgnoreCase);
            if (directoryChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "DirectoryPath",
                    OldValue: renameItem.Original.DirectoryPath,
                    NewValue: renameItem.Preview.DirectoryPath));
            }

            return changes;
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
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="recursiveDirectoryFileAdd">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        /// <returns>Resolved file paths for the source.</returns>
        private static IEnumerable<string> _ResolveSource(
            string source,
            bool includeFolders,
            bool recursiveDirectoryFileAdd)
        {
            var fullSource = Path.GetFullPath(source);
            if (Directory.Exists(fullSource))
            {
                if (includeFolders)
                {
                    // Directory sources resolve to the directory path itself when folders are included.
                    return [fullSource];
                }

                // When folder entries are excluded, directory sources expand to files based on recursion mode.
                var searchOption = recursiveDirectoryFileAdd
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;
                return Directory.EnumerateFiles(fullSource, "*", searchOption);
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
                .Where(item => item.Status == RenameStatus.PreviewOk)
                .ToList();
            var sourcePathToIsMoving = candidateItems
                .Select(item => item.Original.FullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var destinationPathToIsDuplicate = candidateItems
                .Select(item => item.Preview.FullPath)
                .GroupBy(destinationPath => destinationPath, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in candidateItems)
            {
                var destinationPath = item.Preview.FullPath;
                var destinationPathIsDuplicate = destinationPathToIsDuplicate.Contains(destinationPath);
                if (destinationPathIsDuplicate)
                {
                    item.SetPreviewError(
                        message: $"Preview conflict for destination '{destinationPath}'.",
                        cause: null);
                    Log.Warning(
                        "Preview conflict for destination '{DestinationPath}' (duplicate destination).",
                        destinationPath);
                    continue;
                }

                var destinationExistsOutsideBatch = File.Exists(destinationPath)
                    && !sourcePathToIsMoving.Contains(destinationPath);
                if (destinationExistsOutsideBatch)
                {
                    item.SetPreviewError(
                        message: $"Preview conflict for destination '{destinationPath}'.",
                        cause: null);
                    Log.Warning(
                        "Preview conflict for destination '{DestinationPath}' (path already exists).",
                        destinationPath);
                    continue;
                }
            }
        }

        /// <summary>
        /// Appends resolved paths to <see cref="RenameItems"/> while enforcing deduplication and filtering.
        /// </summary>
        /// <param name="resolvedPaths">Resolved file paths to append.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <returns>The count of newly added resolved items.</returns>
        private int _AppendPaths(IEnumerable<string> resolvedPaths, bool includeFiles, bool includeFolders)
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

                var isDirectory = attrs.HasFlag(FileAttributes.Directory);
                if (isDirectory && !includeFolders)
                {
                    continue;
                }

                if (!isDirectory && !includeFiles)
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
