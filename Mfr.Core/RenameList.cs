using Mfr.Models;
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
                AddSource(
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
            var resolvedPaths = RenameSourceResolver.Resolve(
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
        /// <param name="preset">The rename preset (ordered filter chain).</param>
        /// <remarks>
        /// Call <see cref="FilterChain.SetupFilters"/> on <see cref="FilterPreset.Chain"/> before this method
        /// so filter setup runs once for the chain (for example from the CLI before preview).
        /// </remarks>
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
                    preset.Chain.ApplyFilters(renameItem);
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
                renameItem.LogPreviewChangeDetail();
            }

            _LogPreviewOutcomeSummary(_renameItems);
        }

        /// <summary>
        /// Commits previously previewed rename operations.
        /// </summary>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <param name="dryRun">If <c>true</c>, simulates commit outcomes without applying filesystem changes.</param>
        /// <param name="confirmBeforeApply">
        /// Optional callback invoked only for items whose preview path differs from the original.
        /// Return <c>false</c> to skip that item with status <see cref="RenameStatus.CommitSkipped"/> without treating it as an error.
        /// </param>
        /// <returns>Per-item commit outcomes including success, skipped, and errors.</returns>
        public IReadOnlyList<RenameResultItem> Commit(
            bool failFast,
            bool dryRun = false,
            Func<RenameItem, bool>? confirmBeforeApply = null)
        {
            Log.Information(
                "Starting commit for {ItemCount} item(s). FailFast: {FailFast}. DryRun: {DryRun}. ConfirmBeforeApply: {HasConfirmBeforeApply}.",
                _renameItems.Count,
                failFast,
                dryRun,
                confirmBeforeApply is not null);

            var results = new List<RenameResultItem>(_renameItems.Count);
            var stopped = false;

            foreach (var item in _renameItems)
            {
                item.CommitError = null;
                var sourcePath = item.Original.FullPath;

                var shouldSkipCommit =
                    stopped
                    || !item.HasPreviewChanges()
                    || (confirmBeforeApply is not null
                        && !item.IsPreviewPathSameAsOriginal()
                        && !confirmBeforeApply(item));
                if (shouldSkipCommit)
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
                var attributesBeforeCommit = item.Original.Attributes;
                var attributesAfterPreview = item.Preview.Attributes;
                try
                {
                    if (!dryRun)
                    {
                        item.Commit();
                    }

                    item.Status = RenameStatus.CommitOk;
                    var changes = _BuildCommitChanges(
                        sourcePath: sourcePath,
                        destinationPath: destPath,
                        originalAttributes: attributesBeforeCommit,
                        previewAttributes: attributesAfterPreview);
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
        /// Builds property change rows for a committed item (file name and optional attributes).
        /// </summary>
        /// <param name="sourcePath">Original source path.</param>
        /// <param name="destinationPath">Destination path.</param>
        /// <param name="originalAttributes">Attributes before commit.</param>
        /// <param name="previewAttributes">Attributes after preview (applied on commit).</param>
        /// <returns>Property-level changes for result reporting.</returns>
        private static List<RenamePropertyChange> _BuildCommitChanges(
            string sourcePath,
            string destinationPath,
            FileAttributes originalAttributes,
            FileAttributes previewAttributes)
        {
            var changes = new List<RenamePropertyChange>();
            var sourceFileName = Path.GetFileName(sourcePath);
            var destinationFileName = Path.GetFileName(destinationPath);
            var fileNameChanged = !string.Equals(sourceFileName, destinationFileName, StringComparison.OrdinalIgnoreCase);
            if (fileNameChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "FileName",
                    OldValue: sourceFileName,
                    NewValue: destinationFileName));
            }

            if (originalAttributes != previewAttributes)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Attributes",
                    OldValue: originalAttributes.ToString(),
                    NewValue: previewAttributes.ToString()));
            }

            return changes;
        }

        /// <summary>
        /// Counts preview results and writes the finished-preview log line.
        /// </summary>
        private static void _LogPreviewOutcomeSummary(IEnumerable<RenameItem> items)
        {
            var changed = 0;
            var unchanged = 0;
            var errors = 0;
            foreach (var item in items)
            {
                if (item.Status == RenameStatus.PreviewError)
                {
                    errors++;
                    continue;
                }

                if (item.Status != RenameStatus.PreviewOk)
                {
                    continue;
                }

                if (item.HasPreviewChanges())
                {
                    changed++;
                }
                else
                {
                    unchanged++;
                }
            }

            Log.Information(
                "Finished preview. Changed: {PreviewChangedCount}, Unchanged: {PreviewUnchangedCount}, Errors: {PreviewErrorCount}.",
                changed,
                unchanged,
                errors);
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
                    extension: extension,
                    attributes: attrs);
                var renameItem = new RenameItem(fileMeta);
                _renameItems.Add(renameItem);
                addedCount++;
            }

            return addedCount;
        }
    }
}
