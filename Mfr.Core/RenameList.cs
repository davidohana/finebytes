using Mfr.Metadata;
using Mfr.Models;
using Mfr.Utils;
using Serilog;

namespace Mfr.Core
{
    /// <summary>
    /// Maintains ordered rename sources and resolves them into file entries.
    /// </summary>
    /// <param name="includeHidden">If <c>true</c>, includes hidden/system files while resolving.</param>
    public sealed class RenameList(bool includeHidden)
    {
        private readonly HashSet<string> _resolvedPathToIsIncluded = new(PathComparers.Os);
        private readonly List<RenameItem> _renameItems = [];
        private readonly Dictionary<string, int> _folderPathToCount = new(PathComparers.Os);
        private readonly bool _includeHidden = includeHidden;
        private CommitPlan? _commitPlan;

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
        /// <param name="includeSubdirs">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        public void AddSources(
            IEnumerable<string> sources,
            bool includeFiles = true,
            bool includeFolders = true,
            bool includeSubdirs = false)
        {
            var sourceList = sources.ToList();
            Log.Information(
                "Received {SourceCount} source(s) for resolution. IncludeFiles: {IncludeFiles}, IncludeFolders: {IncludeFolders}, IncludeSubdirs: {IncludeSubdirs}, IncludeHidden: {IncludeHidden}.",
                sourceList.Count,
                includeFiles,
                includeFolders,
                includeSubdirs,
                _includeHidden);

            foreach (var source in sourceList)
            {
                AddSource(
                    source: source,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    includeSubdirs: includeSubdirs);
            }
        }

        /// <summary>
        /// Adds and resolves a single source.
        /// </summary>
        /// <param name="source">A file path, directory path, or wildcard source.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="includeSubdirs">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        /// <returns>The count of newly added resolved items.</returns>
        public int AddSource(
            string source,
            bool includeFiles = true,
            bool includeFolders = true,
            bool includeSubdirs = false)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new UserException("Source cannot be empty.");

            var trimmedSource = source.Trim();
            var fullSource = Path.GetFullPath(trimmedSource);
            var isRootPath = string.Equals(
                Path.GetPathRoot(fullSource),
                fullSource,
                PathComparers.OsComparison);
            if (isRootPath)
                throw new UserException($"Root paths cannot be added as rename sources: '{trimmedSource}'.");

            var resolvedPaths = PathResolver.ResolveToPaths(
                            source: trimmedSource,
                            includeFolders: includeFolders,
                            includeSubdirs: includeSubdirs).ToList();
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

            _PopulateRenameListCounterContext();

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

            RenamePreviewFolderRebaser.RebaseDescendants(_renameItems);
            RenameConflictDetector.MarkConflicts(_renameItems);

            _commitPlan = RenameCommitPlanner.Build(_renameItems);
            foreach (var unresolvableCycleItem in _commitPlan.UnresolvableCycleItems)
            {
                unresolvableCycleItem.SetPreviewError(
                    message: $"Could not resolve rename cycle for '{unresolvableCycleItem.Original.FullPath}'.",
                    cause: null);
            }

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
        /// Optional callback invoked immediately before each item is committed.
        /// Receives every item that has preview changes, including attribute-only changes.
        /// Return <c>false</c> to skip that item with status <see cref="RenameStatus.CommitSkipped"/> without treating it as an error.
        /// Items in an unresolvable cycle that are already in flight (stashed to a temp path) bypass this callback to avoid orphaned files.
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

            if (_commitPlan is null)
            {
                throw new InvalidOperationException(
                    "Preview must be called before Commit.");
            }

            var results = RenameCommitExecutor.Execute(
                plan: _commitPlan,
                allItems: _renameItems,
                confirmBeforeApply: confirmBeforeApply,
                failFast: failFast,
                dryRun: dryRun);

            foreach (var item in _renameItems)
            {
                item.ClearPreview();
            }

            foreach (var item in _renameItems)
            {
                item.ClearAudioTagsCache();
            }

            _commitPlan = null;

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
        /// Counts preview results and writes the finished-preview log line.
        /// </summary>
        private static void _LogPreviewOutcomeSummary(IEnumerable<RenameItem> items)
        {
            var itemList = items.ToList();
            var errors = itemList.Count(i => i.Status == RenameStatus.PreviewError);
            var okItems = itemList.Where(i => i.Status == RenameStatus.PreviewOk).ToList();
            var changed = okItems.Count(i => i.HasPreviewChanges());
            var unchanged = okItems.Count(i => !i.HasPreviewChanges());

            Log.Information(
                "Finished preview. Changed: {PreviewChangedCount}, Unchanged: {PreviewUnchangedCount}, Errors: {PreviewErrorCount}.",
                changed,
                unchanged,
                errors);
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
                    continue;

                var attrs = File.GetAttributes(fullPath);
                if (!_includeHidden &&
                    (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System)))
                {
                    continue;
                }

                var isDirectory = attrs.IsDirectory();
                if (isDirectory && !includeFolders)
                    continue;

                if (!isDirectory && !includeFiles)
                    continue;

                if (isDirectory)
                {
                    var resolvedRoot = Path.GetPathRoot(fullPath) ?? string.Empty;
                    var isResolvedRootPath = string.Equals(
                        _NormalizePathKey(resolvedRoot),
                        normalizedResolvedPath,
                        StringComparison.Ordinal);
                    if (isResolvedRootPath)
                    {
                        Log.Warning("Skipping root path '{Path}': root paths cannot be renamed.", fullPath);
                        continue;
                    }
                }

                string directoryPath;
                string prefix;
                string extension;

                if (isDirectory)
                    (directoryPath, prefix, extension) = _SplitRenamePathForDirectory(fullPath);

                else
                    (directoryPath, prefix, extension) = _SplitRenamePathForFile(fullPath);

                var inFolderIndex = _folderPathToCount.GetValueOrDefault(directoryPath);
                _folderPathToCount[directoryPath] = inFolderIndex + 1;

                var renameListIndex = _renameItems.Count;
                var originalFileMeta = new FileMeta(
                    renameListIndex: renameListIndex,
                    inFolderIndex: inFolderIndex,
                    directoryPath: directoryPath,
                    prefix: prefix,
                    extension: extension,
                    attributes: attrs,
                    creationTime: File.GetCreationTime(fullPath),
                    lastWriteTime: File.GetLastWriteTime(fullPath),
                    lastAccessTime: File.GetLastAccessTime(fullPath),
                    fileSize: isDirectory ? 0 : new FileInfo(fullPath).Length);

                var renameItem = new RenameItem(originalFileMeta, AudioTagPersistence.Read);
                _renameItems.Add(renameItem);
                addedCount++;
            }

            return addedCount;
        }

        /// <summary>
        /// Fills rename-list sizing fields on each item so formatter tokens (for example <c>&lt;counter&gt;</c>
        /// automatic width) can resolve consistently during preview.
        /// </summary>
        private void _PopulateRenameListCounterContext()
        {
            var total = _renameItems.Count;
            foreach (var item in _renameItems)
            {
                var dir = item.Original.DirectoryPath;
                var folderTotal = _folderPathToCount.TryGetValue(dir, out var n) ? n : 1;
                item.Original.RenameListTotalCount = total;
                item.Original.RenameListFolderSiblingCount = folderTotal;
            }
        }

        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path key.</returns>
        private static string _NormalizePathKey(string path)
        {
            var normalized = Path.GetFullPath(path);
            return OperatingSystem.IsWindows() ? normalized.Replace('/', '\\') : normalized;
        }

        /// <summary>
        /// Splits a file path into rename metadata using file-style prefix and extension.
        /// </summary>
        private static (string DirectoryPath, string Prefix, string Extension) _SplitRenamePathForFile(
            string fullPath)
        {
            var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            return (directoryPath, prefix, extension);
        }

        /// <summary>
        /// Splits a directory path into parent directory, full final segment as prefix, and empty extension.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Dotted folder names (for example <c>release.v2</c>) must not be split the way
        /// <see cref="Path.GetFileNameWithoutExtension(string)"/> splits file names.
        /// </para>
        /// </remarks>
        private static (string DirectoryPath, string Prefix, string Extension) _SplitRenamePathForDirectory(
            string fullPath)
        {
            var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var directoryPath = Path.GetDirectoryName(trimmed) ?? "";
            var prefix = Path.GetFileName(trimmed);
            return (directoryPath, prefix, string.Empty);
        }
    }
}
