using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Maintains ordered rename sources and resolves them into file entries.
    /// </summary>
    /// <param name="includeHidden">If <c>true</c>, includes hidden/system files while resolving.</param>
    public sealed class RenameList(bool includeHidden)
    {
        /// <summary>
        /// Normalized full paths already present in <see cref="RenameItems"/>; used to dedupe adds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Paths are OS-normalized via <c>_NormalizePathKey</c>. Removed on
        /// <see cref="Remove(RenameItem)"/> and cleared on <see cref="Clear"/> so a path can be added again.
        /// </para>
        /// </remarks>
        private readonly HashSet<string> _includedResolvedPaths = new(PathComparers.Os);
        private readonly List<RenameItem> _renameItems = [];

        /// <summary>
        /// Parent directory path to how many rename items share that directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used when assigning <see cref="FileMeta.InFolderIndex"/> and rebuilt by
        /// <c>_ReindexItems</c> after add, remove, or move. Also feeds <see cref="FileMeta.RenameListFolderSiblingCount"/>
        /// during preview (for example per-folder <c>&lt;counter&gt;</c> width).
        /// </para>
        /// </remarks>
        private readonly Dictionary<string, int> _folderPathToCount = new(PathComparers.Os);
        private readonly bool _includeHidden = includeHidden;

        /// <summary>
        /// Gets the resolved file items in current list order.
        /// </summary>
        public IReadOnlyList<RenameItem> RenameItems => _renameItems;

        /// <summary>
        /// Adds multiple sources while preserving insertion order.
        /// </summary>
        /// <param name="sources">Sources to add.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="includeSubdirs">Whether directory expansion should recurse into subdirectories.</param>
        /// <param name="excludeMasks">Exclusive file-name masks for discovered directory entries.</param>
        /// <param name="cancellationToken">When canceled, stops resolution and returns without throwing.</param>
        /// <param name="progress">Optional progress sink (scanned / added / last path).</param>
        /// <param name="insertAtIndex">
        /// 0-based index to insert new items; <see langword="null"/> appends at the end.
        /// </param>
        /// <returns>Summary of sources that were skipped during resolution.</returns>
        /// <remarks>
        /// <para>
        /// One call builds a <c>batch</c>: a staging list of new <see cref="RenameItem"/>s that is not
        /// part of <see cref="RenameItems"/> yet. Paths are reserved in the dedupe set while they sit
        /// in the batch. On success the batch is inserted at <paramref name="insertAtIndex"/> and
        /// reindexed; on cancel or unexpected failure it is discarded (dedupe keys released) so the
        /// live list never shows a partial add.
        /// </para>
        /// </remarks>
        public RenameListAddSummary AddSources(
            IEnumerable<string> sources,
            bool includeFiles = true,
            bool includeFolders = true,
            bool includeSubdirs = false,
            IReadOnlyList<string>? excludeMasks = null,
            CancellationToken cancellationToken = default,
            IProgress<RenameListAddProgress>? progress = null,
            int? insertAtIndex = null
        )
        {
            var sourceList = sources.ToList();
            Log.Information(
                "Received {SourceCount} source(s) for resolution. IncludeFiles: {IncludeFiles}, IncludeFolders: {IncludeFolders}, IncludeSubdirs: {IncludeSubdirs}, IncludeHidden: {IncludeHidden}, InsertAtIndex: {InsertAtIndex}.",
                sourceList.Count,
                includeFiles,
                includeFolders,
                includeSubdirs,
                _includeHidden,
                insertAtIndex
            );

            var tracker = new AddProgressTracker(progress, cancellationToken);
            var skippedSourceCount = 0;
            var insertAt = insertAtIndex ?? _renameItems.Count;
            var batch = new List<RenameItem>();
            try
            {
                foreach (var source in sourceList)
                {
                    if (tracker.IsCanceled)
                    {
                        break;
                    }

                    if (
                        !_TryAddSource(
                            source: source,
                            includeFiles: includeFiles,
                            includeFolders: includeFolders,
                            includeSubdirs: includeSubdirs,
                            excludeMasks: excludeMasks,
                            tracker: tracker,
                            batch: batch
                        )
                    )
                    {
                        skippedSourceCount++;
                    }
                }

                if (tracker.IsCanceled)
                {
                    _DiscardCollectedItems(batch);
                }
                else if (batch.Count > 0)
                {
                    _renameItems.InsertRange(insertAt, batch);
                    _ReindexItems();
                }
            }
            catch
            {
                _DiscardCollectedItems(batch);
                throw;
            }

            tracker.ReportFinal();
            return new RenameListAddSummary(skippedSourceCount);
        }

        /// <summary>
        /// Adds and resolves a single source into the staging <paramref name="batch"/>.
        /// </summary>
        /// <param name="batch">Staging list for this <see cref="AddSources"/> call (see that method's remarks).</param>
        /// <returns><see langword="false"/> when the source was skipped; otherwise <see langword="true"/>.</returns>
        private bool _TryAddSource(
            string source,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            IReadOnlyList<string>? excludeMasks,
            AddProgressTracker tracker,
            List<RenameItem> batch
        )
        {
            try
            {
                _AddSource(
                    source: source,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders,
                    includeSubdirs: includeSubdirs,
                    excludeMasks: excludeMasks,
                    tracker: tracker,
                    batch: batch
                );
                return true;
            }
            catch (UserException ex)
            {
                Log.Warning(ex, "Skipped rename source '{Source}'.", source);
                return false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log.Warning(ex, "Skipped rename source '{Source}'.", source);
                return false;
            }
        }

        /// <summary>
        /// Resolves one source and appends accepted items to the staging <paramref name="batch"/>.
        /// </summary>
        /// <param name="batch">Staging list for this <see cref="AddSources"/> call (see that method's remarks).</param>
        private void _AddSource(
            string source,
            bool includeFiles,
            bool includeFolders,
            bool includeSubdirs,
            IReadOnlyList<string>? excludeMasks,
            AddProgressTracker tracker,
            List<RenameItem> batch
        )
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new UserException("Source cannot be empty.");
            }

            var trimmedSource = source.Trim();
            var fullSource = Path.GetFullPath(trimmedSource);
            var isRootPath = string.Equals(Path.GetPathRoot(fullSource), fullSource, PathComparers.OsComparison);
            if (isRootPath)
            {
                throw new UserException($"Root paths cannot be added as rename sources: '{trimmedSource}'.");
            }

            var resolvedPaths = AddedSourceResolver.ResolveToPaths(
                source: trimmedSource,
                includeFiles: includeFiles,
                includeFolders: includeFolders,
                includeSubdirs: includeSubdirs,
                excludeMasks: excludeMasks,
                cancellationToken: tracker.Token
            );
            var addedCount = _CollectResolvedItems(
                resolvedPaths: resolvedPaths,
                includeFiles: includeFiles,
                includeFolders: includeFolders,
                tracker: tracker,
                batch: batch
            );
            Log.Information(
                "Resolved source '{Source}', added {AddedCount} new item(s) (scanned {ScannedCount}).",
                trimmedSource,
                addedCount,
                tracker.ScannedCount
            );
        }

        /// <summary>
        /// Drops a staging batch that was never inserted into <see cref="RenameItems"/>.
        /// </summary>
        /// <param name="batch">Items reserved during the walk but not committed to the live list.</param>
        /// <remarks>
        /// <para>
        /// Only clears their paths from the dedupe set so a later add can accept them again. The batch
        /// itself was never appended to <see cref="RenameItems"/>.
        /// </para>
        /// </remarks>
        private void _DiscardCollectedItems(List<RenameItem> batch)
        {
            foreach (var item in batch)
            {
                _includedResolvedPaths.Remove(_NormalizePathKey(item.Original.FullPath));
            }
        }

        /// <summary>
        /// Removes a resolved item from the list.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>1</c> when the item was removed; otherwise <c>0</c>.</returns>
        public int Remove(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return Remove([item]);
        }

        /// <summary>
        /// Removes multiple resolved items from the list.
        /// </summary>
        /// <param name="items">Items to remove; entries not in the list are ignored.</param>
        /// <returns>The count of items removed.</returns>
        public int Remove(IEnumerable<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var itemsToRemove = items as IReadOnlyCollection<RenameItem> ?? [.. items];
            if (itemsToRemove.Count == 0)
            {
                return 0;
            }

            var removeSet = new HashSet<RenameItem>(itemsToRemove);
            var indicesToRemove = new List<int>();
            for (var i = 0; i < _renameItems.Count; i++)
            {
                if (removeSet.Contains(_renameItems[i]))
                {
                    indicesToRemove.Add(i);
                }
            }

            if (indicesToRemove.Count == 0)
            {
                return 0;
            }

            for (var i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                var index = indicesToRemove[i];
                var item = _renameItems[index];
                _includedResolvedPaths.Remove(_NormalizePathKey(item.Original.FullPath));
                _renameItems.RemoveAt(index);
            }

            _ReindexItems();
            Log.Information("Removed {RemovedCount} item(s) from rename list.", indicesToRemove.Count);
            return indicesToRemove.Count;
        }

        /// <summary>
        /// Removes all resolved items from the list.
        /// </summary>
        public void Clear()
        {
            var removedCount = _renameItems.Count;
            if (removedCount == 0)
            {
                return;
            }

            _renameItems.Clear();
            _includedResolvedPaths.Clear();
            _folderPathToCount.Clear();
            Log.Information("Cleared rename list ({RemovedCount} item(s)).", removedCount);
        }

        /// <summary>
        /// Moves the given items one position by <paramref name="offset"/>.
        /// </summary>
        /// <param name="items">Items to move; entries not in the list are ignored.</param>
        /// <param name="offset">Negative moves toward the start; positive toward the end.</param>
        /// <returns><see langword="true"/> when at least one item changed position.</returns>
        /// <remarks>
        /// <para>
        /// Contiguous selected blocks move as a unit. Items already at the list edge (or blocked by
        /// another selected neighbor in that direction) stay put. Matches MFR7 manual sort.
        /// </para>
        /// </remarks>
        public bool MoveSelected(IEnumerable<RenameItem> items, int offset)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (offset is not (-1 or 1))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be -1 or 1.");
            }

            var selected = items.ToHashSet();
            if (selected.Count == 0 || _renameItems.Count == 0)
            {
                return false;
            }

            var moved = false;
            var walkStep = -offset;
            var startIndex = walkStep > 0 ? 0 : _renameItems.Count - 1;
            for (var i = startIndex; i >= 0 && i < _renameItems.Count; i += walkStep)
            {
                if (!_CanSwapTowardNeighbor(selected, i, offset))
                {
                    continue;
                }

                var neighborIndex = i + offset;
                (_renameItems[i], _renameItems[neighborIndex]) = (_renameItems[neighborIndex], _renameItems[i]);
                moved = true;
            }

            if (!moved)
            {
                return false;
            }

            _ReindexItems();
            return true;
        }

        /// <summary>
        /// Whether the item at <paramref name="index"/> is selected and can swap with the neighbor.
        /// </summary>
        private bool _CanSwapTowardNeighbor(HashSet<RenameItem> selected, int index, int offset)
        {
            if (!selected.Contains(_renameItems[index]))
            {
                return false;
            }

            var neighborIndex = index + offset;
            if (neighborIndex < 0 || neighborIndex >= _renameItems.Count)
            {
                return false;
            }

            return !selected.Contains(_renameItems[neighborIndex]);
        }

        /// <summary>
        /// Previews rename outcomes for the current list without touching the filesystem.
        /// </summary>
        /// <param name="preset">The rename preset (ordered filter chain).</param>
        /// <returns>The commit plan for the previewed items; pass this to <see cref="Commit"/>.</returns>
        /// <remarks>
        /// <para>
        /// Call <see cref="FilterChain.SetupFilters"/> on <see cref="FilterPreset.Chain"/> before this method
        /// so filter setup runs once for the chain (for example from the CLI before preview).
        /// </para>
        /// </remarks>
        public CommitPlan Preview(FilterPreset preset)
        {
            Log.Information(
                "Starting preview for preset '{PresetName}' with {ItemCount} item(s).",
                preset.Name,
                _renameItems.Count
            );

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

                    if (renameItem.PreviewError is null)
                    {
                        renameItem.Status = RenameStatus.PreviewOk;
                    }
                }
                catch (Exception ex)
                {
                    renameItem.SetPreviewError(message: ex.Message, cause: ex);
                    Log.Warning(ex, "Preview failed for '{SourcePath}'.", renameItem.Original.FullPath);
                }
            }

            RenamePreviewFolderRebaser.RebaseDescendants(_renameItems);
            PreviewConflictDetector.MarkConflicts(_renameItems);

            var commitPlan = CommitPlanner.Build(_renameItems);
            foreach (var unresolvableCycleItem in commitPlan.UnresolvableCycleItems)
            {
                unresolvableCycleItem.SetPreviewError(
                    message: $"Could not resolve rename cycle for '{unresolvableCycleItem.Original.FullPath}'.",
                    cause: null
                );
            }

            foreach (var renameItem in _renameItems)
            {
                renameItem.LogPreviewChangeDetail();
            }

            _LogPreviewOutcomeSummary(_renameItems);

            return commitPlan;
        }

        /// <summary>
        /// Commits previously previewed rename operations.
        /// </summary>
        /// <param name="plan">The plan returned by <see cref="Preview"/> for this list.</param>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <param name="dryRun">If <c>true</c>, simulates commit outcomes without applying filesystem changes.</param>
        /// <param name="confirmBeforeApply">
        /// Optional callback invoked immediately before each item is committed.
        /// Receives every item that has preview changes, including attribute-only changes.
        /// Return <c>false</c> to skip that item with status <see cref="RenameStatus.CommitSkipped"/> without treating it as an error.
        /// Items in an unresolvable cycle that are already in flight (stashed to a temp path) bypass this callback to avoid orphaned files.
        /// </param>
        /// <returns>Per-item commit outcomes including success, skipped, and errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="plan"/> is <c>null</c>.</exception>
        public IReadOnlyList<RenameResultItem> Commit(
            CommitPlan plan,
            bool failFast,
            bool dryRun = false,
            Func<RenameItem, bool>? confirmBeforeApply = null
        )
        {
            ArgumentNullException.ThrowIfNull(plan);

            Log.Information(
                "Starting commit for {ItemCount} item(s). FailFast: {FailFast}. DryRun: {DryRun}. ConfirmBeforeApply: {HasConfirmBeforeApply}.",
                _renameItems.Count,
                failFast,
                dryRun,
                confirmBeforeApply is not null
            );

            var results = CommitExecutor.Execute(
                plan: plan,
                allItems: _renameItems,
                confirmBeforeApply: confirmBeforeApply,
                failFast: failFast,
                dryRun: dryRun
            );

            foreach (var item in _renameItems)
            {
                item.ClearPreview();
            }

            foreach (var item in _renameItems)
            {
                item.ClearMetadataCaches();
            }

            var commitOkCount = results.Count(item => item.Status == RenameStatus.CommitOk);
            var commitSkippedCount = results.Count(item => item.Status == RenameStatus.CommitSkipped);
            var commitErrorCount = results.Count(item => item.Status == RenameStatus.CommitError);
            Log.Information(
                "Finished commit. Success: {CommitOkCount}, Skipped: {CommitSkippedCount}, Errors: {CommitErrorCount}.",
                commitOkCount,
                commitSkippedCount,
                commitErrorCount
            );

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
                errors
            );
        }

        /// <summary>
        /// Appends accepted resolved paths to the staging <paramref name="batch"/>.
        /// </summary>
        /// <param name="resolvedPaths">Resolved file paths to collect.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="tracker">Shared progress and cancel state for this add operation.</param>
        /// <param name="batch">
        /// Staging list for this <see cref="AddSources"/> call; not yet in <see cref="RenameItems"/>.
        /// </param>
        /// <returns>The count of items appended to <paramref name="batch"/>.</returns>
        private int _CollectResolvedItems(
            IEnumerable<string> resolvedPaths,
            bool includeFiles,
            bool includeFolders,
            AddProgressTracker tracker,
            List<RenameItem> batch
        )
        {
            var addedCount = 0;
            foreach (var fullPath in resolvedPaths)
            {
                if (tracker.IsCanceled)
                {
                    return addedCount;
                }

                tracker.OnScanned(fullPath);

                var normalizedResolvedPath = _NormalizePathKey(fullPath);
                if (!_includedResolvedPaths.Add(normalizedResolvedPath))
                {
                    continue;
                }

                var attrs = File.GetAttributes(fullPath);
                if (!_includeHidden && (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System)))
                {
                    continue;
                }

                var isDirectory = attrs.IsDirectory();
                if (isDirectory && !includeFolders)
                {
                    continue;
                }

                if (!isDirectory && !includeFiles)
                {
                    continue;
                }

                if (isDirectory)
                {
                    var resolvedRoot = Path.GetPathRoot(fullPath) ?? string.Empty;
                    var isResolvedRootPath = string.Equals(
                        _NormalizePathKey(resolvedRoot),
                        normalizedResolvedPath,
                        StringComparison.Ordinal
                    );
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
                {
                    (directoryPath, prefix, extension) = _SplitRenamePathForDirectory(fullPath);
                }
                else
                {
                    (directoryPath, prefix, extension) = _SplitRenamePathForFile(fullPath);
                }

                // Indices are filled by _ReindexItems after the batch is committed to RenameItems.
                var originalFileMeta = new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: directoryPath,
                    prefix: prefix,
                    extension: extension,
                    attributes: attrs,
                    creationTime: File.GetCreationTime(fullPath),
                    lastWriteTime: File.GetLastWriteTime(fullPath),
                    lastAccessTime: File.GetLastAccessTime(fullPath),
                    fileSize: isDirectory ? 0 : new FileInfo(fullPath).Length
                );

                var renameItem = new RenameItem(originalFileMeta);
                batch.Add(renameItem);
                tracker.OnAdded(fullPath);
                addedCount++;
            }

            return addedCount;
        }

        /// <summary>
        /// Reassigns list and per-folder indices after add, remove, or move.
        /// </summary>
        private void _ReindexItems()
        {
            _folderPathToCount.Clear();
            for (var i = 0; i < _renameItems.Count; i++)
            {
                var item = _renameItems[i];
                var directoryPath = item.Original.DirectoryPath;
                var inFolderIndex = _folderPathToCount.GetValueOrDefault(directoryPath);
                _folderPathToCount[directoryPath] = inFolderIndex + 1;
                item.Original.RenameListIndex = i;
                item.Original.InFolderIndex = inFolderIndex;
            }
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
        private static (string DirectoryPath, string Prefix, string Extension) _SplitRenamePathForFile(string fullPath)
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
            string fullPath
        )
        {
            var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var directoryPath = Path.GetDirectoryName(trimmed) ?? "";
            var prefix = Path.GetFileName(trimmed);
            return (directoryPath, prefix, string.Empty);
        }
    }
}
