using Mfr.Filters;
using Mfr.Utils;
using Serilog;
using Serilog.Events;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Maintains ordered rename sources and resolves them into file entries.
    /// </summary>
    public sealed class RenameList
    {
        /// <summary>
        /// Normalized full paths already present in <see cref="RenameItems"/>; used to dedupe adds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Paths are OS-normalized via <c>NormalizePathKey</c>. Removed on
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

        /// <summary>
        /// Gets the resolved file items in current list order.
        /// </summary>
        public IReadOnlyList<RenameItem> RenameItems => _renameItems;

        internal static string NormalizePathKey(string path)
        {
            var normalized = Path.GetFullPath(path);
            return OperatingSystem.IsWindows() ? normalized.Replace('/', '\\') : normalized;
        }

        /// <summary>
        /// Adds multiple sources while preserving insertion order.
        /// </summary>
        /// <param name="sources">Sources to add.</param>
        /// <param name="includeFiles">Whether file entries should be included from resolved paths.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="includeSubdirs">Whether directory expansion should recurse into subdirectories.</param>
        /// <param name="includeHidden">Whether hidden and system items are included while resolving.</param>
        /// <param name="excludeMasks">Exclusive file-name masks for discovered directory entries.</param>
        /// <param name="cancellationToken">When canceled, stops resolution and returns without throwing.</param>
        /// <param name="progress">Optional progress sink (resolve counts, metadata counts, last path).</param>
        /// <param name="insertAtIndex">
        /// 0-based index to insert new items; <see langword="null"/> appends at the end.
        /// </param>
        /// <param name="metadataRequirement">
        /// Metadata buckets to hydrate on the staging batch before insert; default is none (CLI / filter preview stay lazy).
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
            bool includeHidden = false,
            IReadOnlyList<string>? excludeMasks = null,
            CancellationToken cancellationToken = default,
            IProgress<RenameListProgress>? progress = null,
            int? insertAtIndex = null,
            RenameListMetadataRequirement metadataRequirement = RenameListMetadataRequirement.None
        )
        {
            var sourceList = sources.ToList();
            Log.Information(
                "Received {SourceCount} source(s) for resolution. IncludeFiles: {IncludeFiles}, IncludeFolders: {IncludeFolders}, IncludeSubdirs: {IncludeSubdirs}, IncludeHidden: {IncludeHidden}, InsertAtIndex: {InsertAtIndex}.",
                sourceList.Count,
                includeFiles,
                includeFolders,
                includeSubdirs,
                includeHidden,
                insertAtIndex
            );

            var tracker = new RenameListProgressTracker(progress, cancellationToken);
            var resolveOptions = new SourceResolveOptions(
                IncludeFiles: includeFiles,
                IncludeFolders: includeFolders,
                IncludeSubdirs: includeSubdirs,
                IncludeHidden: includeHidden,
                ExcludeMasks: excludeMasks
            );
            var batch = new List<RenameItem>();
            var skippedSourceCount = 0;
            var inserted = false;
            try
            {
                skippedSourceCount = RenameListBatchResolver.FillBatch(
                    sourceList,
                    resolveOptions,
                    tracker,
                    batch,
                    _includedResolvedPaths
                );
                if (!tracker.IsCanceled)
                {
                    _EnsureMetadataLoaded(batch, metadataRequirement, tracker);
                }

                if (!tracker.IsCanceled)
                {
                    _InsertCollectedItems(batch, insertAtIndex ?? _renameItems.Count);
                    inserted = true;
                }
            }
            finally
            {
                if (!inserted)
                {
                    _DiscardCollectedItems(batch);
                }
            }

            tracker.ReportFinal();
            return new RenameListAddSummary(skippedSourceCount);
        }

        private void _InsertCollectedItems(List<RenameItem> batch, int insertAt)
        {
            if (batch.Count == 0)
            {
                return;
            }

            _renameItems.InsertRange(insertAt, batch);
            _ReindexItems();
        }

        private void _DiscardCollectedItems(List<RenameItem> batch)
        {
            foreach (var item in batch)
            {
                _includedResolvedPaths.Remove(NormalizePathKey(item.Original.FullPath));
            }
        }

        private static void _EnsureMetadataLoaded(
            List<RenameItem> items,
            RenameListMetadataRequirement requirement,
            RenameListProgressTracker tracker
        )
        {
            if (requirement == RenameListMetadataRequirement.None || items.Count == 0)
            {
                return;
            }

            tracker.BeginMetadataPhase(items.Count);
            foreach (var item in items)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                RenameListMetadataLoader.TryEnsureLoaded(item, requirement);
                tracker.OnMetadataProcessed(item.Original.FullPath);
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
                _includedResolvedPaths.Remove(NormalizePathKey(item.Original.FullPath));
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
        /// Removes every item whose preview for <paramref name="key"/> matches the original (MFR7 Remove Unchanged).
        /// </summary>
        /// <param name="key">Preview field key to compare; original keys are a no-op.</param>
        /// <returns>The count of items removed.</returns>
        /// <remarks>
        /// <para>
        /// Collects rows where <see cref="RenameListFieldCatalog.IsPreviewChanged"/> is false, then
        /// delegates to <see cref="Remove(IEnumerable{RenameItem})"/> for path-set and reindex.
        /// </para>
        /// </remarks>
        public int RemoveUnchanged(RenameListFieldKey key)
        {
            if (!key.IsPreview || _renameItems.Count == 0)
            {
                return 0;
            }

            var unchanged = _renameItems.Where(item => !RenameListFieldCatalog.IsPreviewChanged(item, key)).ToList();
            return Remove(unchanged);
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
            if (!ListReorder.TryMoveSelectedTowardNeighbor(_renameItems, selected, offset))
            {
                return false;
            }

            _ReindexItems();
            return true;
        }

        /// <summary>
        /// Moves the given items as a block to insert before <paramref name="beforeItem"/>.
        /// </summary>
        /// <param name="items">Items to move; entries not in the list are ignored. Relative order is preserved.</param>
        /// <param name="beforeItem">
        /// Item to insert before, or <see langword="null"/> to append. When this item is among
        /// <paramref name="items"/>, the call is a no-op (drop onto selection).
        /// </param>
        /// <returns><see langword="true"/> when the list order changed.</returns>
        /// <remarks>
        /// <para>
        /// Matches MFR7 internal drag-reorder: remove the selection, then insert at the marked row
        /// (or at the end when there is no mark target). Does not touch the path dedupe set.
        /// </para>
        /// </remarks>
        public bool MoveSelectedBefore(IEnumerable<RenameItem> items, RenameItem? beforeItem)
        {
            ArgumentNullException.ThrowIfNull(items);

            var selected = items.ToHashSet();
            var dropOnSelection = beforeItem is not null && selected.Contains(beforeItem);
            if (selected.Count == 0 || dropOnSelection)
            {
                return false;
            }

            var toMove = _renameItems.Where(selected.Contains).ToList();
            if (toMove.Count == 0)
            {
                return false;
            }

            _renameItems.RemoveAll(selected.Contains);
            var insertAt = beforeItem is null ? _renameItems.Count : _renameItems.IndexOf(beforeItem);
            if (insertAt < 0)
            {
                insertAt = _renameItems.Count;
            }

            _renameItems.InsertRange(insertAt, toMove);
            _ReindexItems();
            return true;
        }

        /// <summary>
        /// Sorts the list by <paramref name="keys"/> and reindexes.
        /// </summary>
        /// <param name="keys">
        /// Sort keys in priority order. Empty or only non-sortable keys is a no-op.
        /// </param>
        /// <returns><see langword="true"/> when the list order may have changed.</returns>
        public bool Sort(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            if (keys.Count == 0 || _renameItems.Count <= 1)
            {
                return false;
            }

            if (!keys.Any(key => RenameListFieldCatalog.IsSortableKey(key.FieldKey)))
            {
                return false;
            }

            _renameItems.Sort((left, right) => RenameListComparer.CompareItems(left, right, keys));
            _ReindexItems();
            return true;
        }

        /// <summary>
        /// Ensures rename-row metadata buckets are loaded for every item in the list.
        /// </summary>
        /// <param name="requirement">Combined metadata requirement flags; <see cref="RenameListMetadataRequirement.None"/> is a no-op.</param>
        /// <param name="cancellationToken">When canceled, stops without throwing.</param>
        /// <param name="progress">Optional progress sink (metadata processed count, total, last path).</param>
        public void EnsureMetadataLoaded(
            RenameListMetadataRequirement requirement,
            CancellationToken cancellationToken = default,
            IProgress<RenameListProgress>? progress = null
        )
        {
            if (requirement == RenameListMetadataRequirement.None || _renameItems.Count == 0)
            {
                return;
            }

            var tracker = new RenameListProgressTracker(progress, cancellationToken);
            _EnsureMetadataLoaded(_renameItems, requirement, tracker);
            tracker.ReportFinal();
        }

        /// <summary>
        /// Re-reads original filesystem fields for every row and clears lazy metadata caches.
        /// </summary>
        /// <param name="cancellationToken">When canceled, stops without throwing.</param>
        /// <param name="progress">Optional progress sink (metadata processed count, total, last path).</param>
        /// <remarks>
        /// <para>
        /// Does not run preview. Missing paths keep the stored path; field-load errors clear with the cache
        /// so a later hydrate can succeed.
        /// </para>
        /// </remarks>
        public void RefreshOriginals(
            CancellationToken cancellationToken = default,
            IProgress<RenameListProgress>? progress = null
        )
        {
            if (_renameItems.Count == 0)
            {
                return;
            }

            var tracker = new RenameListProgressTracker(progress, cancellationToken);
            var casingCache = new OnDiskCasingCache();
            tracker.BeginMetadataPhase(_renameItems.Count);
            foreach (var item in _renameItems)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                RenameListOriginalsRefresher.RefreshItemOriginal(item, casingCache, _includedResolvedPaths);
                tracker.OnMetadataProcessed(item.Original.FullPath);
            }

            tracker.ReportFinal();
        }

        /// <summary>
        /// Previews rename outcomes for the current list without touching the filesystem.
        /// </summary>
        /// <param name="chain">Ordered filter steps (enabled flags honored).</param>
        /// <param name="cancellationToken">When canceled, stops applying remaining items without throwing.</param>
        /// <param name="progress">Optional progress sink (processed count, total, last path).</param>
        /// <returns>The commit plan for the previewed items; pass this to <see cref="Commit"/>.</returns>
        /// <remarks>
        /// <para>
        /// Calls <see cref="FilterChain.SetupFilters"/> before applying so each enabled filter is set up once.
        /// </para>
        /// <para>
        /// On cancel, items already processed keep their preview; remaining items stay at identity after reset.
        /// Conflict detection and commit planning still run on the partial result.
        /// </para>
        /// </remarks>
        public CommitPlan Preview(
            FilterChain chain,
            CancellationToken cancellationToken = default,
            IProgress<RenameListProgress>? progress = null
        )
        {
            ArgumentNullException.ThrowIfNull(chain);

            chain.SetupFilters();

            _PopulateRenameListCounterContext();

            var tracker = new RenameListProgressTracker(progress, cancellationToken);
            tracker.BeginMetadataPhase(_renameItems.Count);

            foreach (var item in _renameItems)
            {
                item.ResetState();
            }

            foreach (var renameItem in _renameItems)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                try
                {
                    chain.ApplyFilters(renameItem);

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

                tracker.OnMetadataProcessed(renameItem.Original.FullPath);
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

            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                foreach (var renameItem in _renameItems)
                {
                    renameItem.LogPreviewChangeDetail();
                }
            }

            tracker.ReportFinal();

            var (changed, unchanged, errors) = CommitPlan.CountOutcomes(_renameItems);
            return commitPlan with { ChangedCount = changed, UnchangedCount = unchanged, ErrorCount = errors };
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
    }
}
