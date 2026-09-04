using Mfr.Filters;
using Mfr.Utils;
using Serilog;
using Serilog.Events;

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
                _includeHidden,
                insertAtIndex
            );

            var tracker = new RenameListProgressTracker(progress, cancellationToken);
            var resolveOptions = new SourceResolveOptions(
                IncludeFiles: includeFiles,
                IncludeFolders: includeFolders,
                IncludeSubdirs: includeSubdirs,
                ExcludeMasks: excludeMasks
            );
            var batch = new List<RenameItem>();
            var skippedSourceCount = 0;
            var inserted = false;
            try
            {
                skippedSourceCount = _FillBatch(sourceList, resolveOptions, tracker, batch);
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

        /// <summary>
        /// Resolves each source into the staging <paramref name="batch"/> until canceled.
        /// </summary>
        /// <returns>How many sources were skipped (errors), not counting cancel.</returns>
        private int _FillBatch(
            List<string> sourceList,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch
        )
        {
            var skippedSourceCount = 0;
            foreach (var source in sourceList)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                if (!_TryAddSource(source, resolveOptions, tracker, batch))
                {
                    skippedSourceCount++;
                }
            }

            return skippedSourceCount;
        }

        /// <summary>
        /// Inserts a staging batch into <see cref="RenameItems"/> and reindexes.
        /// </summary>
        private void _InsertCollectedItems(List<RenameItem> batch, int insertAt)
        {
            if (batch.Count == 0)
            {
                return;
            }

            _renameItems.InsertRange(insertAt, batch);
            _ReindexItems();
        }

        /// <summary>
        /// Adds and resolves a single source into the staging <paramref name="batch"/>.
        /// </summary>
        /// <returns><see langword="false"/> when the source was skipped; otherwise <see langword="true"/>.</returns>
        private bool _TryAddSource(
            string source,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch
        )
        {
            try
            {
                _AddSource(source, resolveOptions, tracker, batch);
                return true;
            }
            catch (Exception ex)
                when (ex is UserException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log.Warning(ex, "Skipped rename source '{Source}'.", source);
                return false;
            }
        }

        /// <summary>
        /// Resolves one source and appends accepted items to the staging <paramref name="batch"/>.
        /// </summary>
        private void _AddSource(
            string source,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
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
                includeFiles: resolveOptions.IncludeFiles,
                includeFolders: resolveOptions.IncludeFolders,
                includeSubdirs: resolveOptions.IncludeSubdirs,
                excludeMasks: resolveOptions.ExcludeMasks,
                cancellationToken: tracker.Token
            );
            var addedCount = _CollectResolvedItems(
                resolvedPaths: resolvedPaths,
                includeFiles: resolveOptions.IncludeFiles,
                includeFolders: resolveOptions.IncludeFolders,
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
        /// <param name="batch">Items reserved during the walk but not inserted into the live list.</param>
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
        /// Hydrates metadata on a staging or live item list before grid display or sort.
        /// </summary>
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

            _renameItems.Sort((left, right) => _CompareItems(left, right, keys));
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
            // Per-pass only: siblings share directory listings; disk can change between F5 calls.
            var casingCache = new OnDiskCasingCache();
            tracker.BeginMetadataPhase(_renameItems.Count);
            foreach (var item in _renameItems)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                _RefreshItemOriginal(item, casingCache);
                tracker.OnMetadataProcessed(item.Original.FullPath);
            }

            tracker.ReportFinal();
        }

        private static int _CompareItems(RenameItem left, RenameItem right, IReadOnlyList<RenameListSortKey> keys)
        {
            foreach (var key in keys)
            {
                if (!RenameListFieldCatalog.IsSortableKey(key.FieldKey))
                {
                    continue;
                }

                var cmp = RenameListFieldCatalog.CompareForSort(left, key.FieldKey, right);
                if (key.Descending)
                {
                    cmp = -cmp;
                }

                if (cmp != 0)
                {
                    return cmp;
                }
            }

            // MFR7 RenameItemComparer: equal keys keep add order so a second Sort does not reshuffle ties.
            return left.Original.RenameListIndex.CompareTo(right.Original.RenameListIndex);
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
            RenameListProgressTracker tracker,
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

                var renameItem = _TryCreateResolvedItem(
                    fullPath: fullPath,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders
                );
                if (renameItem is null)
                {
                    continue;
                }

                batch.Add(renameItem);
                tracker.OnAdded(fullPath);
                addedCount++;
            }

            return addedCount;
        }

        /// <summary>
        /// Builds a staging item when the path is new and passes include filters; otherwise returns null.
        /// </summary>
        private RenameItem? _TryCreateResolvedItem(string fullPath, bool includeFiles, bool includeFolders)
        {
            var normalizedResolvedPath = _NormalizePathKey(fullPath);
            if (_includedResolvedPaths.Contains(normalizedResolvedPath))
            {
                return null;
            }

            var attrs = File.GetAttributes(fullPath);
            if (
                !_ShouldIncludeResolvedPath(
                    fullPath: fullPath,
                    normalizedResolvedPath: normalizedResolvedPath,
                    attrs: attrs,
                    includeFiles: includeFiles,
                    includeFolders: includeFolders
                )
            )
            {
                return null;
            }

            _includedResolvedPaths.Add(normalizedResolvedPath);
            return _CreateRenameItem(fullPath, attrs);
        }

        /// <summary>
        /// Whether a resolved path should become a rename-list row.
        /// </summary>
        private bool _ShouldIncludeResolvedPath(
            string fullPath,
            string normalizedResolvedPath,
            FileAttributes attrs,
            bool includeFiles,
            bool includeFolders
        )
        {
            var isHiddenOrSystem = attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System);
            if (!_includeHidden && isHiddenOrSystem)
            {
                return false;
            }

            var isDirectory = attrs.IsDirectory();
            if (isDirectory && !includeFolders)
            {
                return false;
            }

            if (!isDirectory && !includeFiles)
            {
                return false;
            }

            if (!isDirectory)
            {
                return true;
            }

            var resolvedRoot = Path.GetPathRoot(fullPath) ?? string.Empty;
            var isResolvedRootPath = string.Equals(
                _NormalizePathKey(resolvedRoot),
                normalizedResolvedPath,
                StringComparison.Ordinal
            );
            if (!isResolvedRootPath)
            {
                return true;
            }

            Log.Warning("Skipping root path '{Path}': root paths cannot be renamed.", fullPath);
            return false;
        }

        /// <summary>
        /// Clears lazy metadata and re-stats <see cref="RenameItem.Original"/> from disk when the path still exists.
        /// </summary>
        private void _RefreshItemOriginal(RenameItem item, OnDiskCasingCache casingCache)
        {
            var priorPath = item.Original.FullPath;
            item.ClearMetadataCaches();

            var resolvedPath = _ResolveExistingPath(priorPath, casingCache);
            if (resolvedPath is null)
            {
                item.SetMissingFromDisk(true);
                return;
            }

            item.SetMissingFromDisk(false);

            var priorKey = _NormalizePathKey(priorPath);
            var resolvedKey = _NormalizePathKey(resolvedPath);
            if (!string.Equals(priorKey, resolvedKey, StringComparison.Ordinal))
            {
                _includedResolvedPaths.Remove(priorKey);
                _includedResolvedPaths.Add(resolvedKey);
            }

            var refreshedOriginal = _CreateOriginalSnapshot(resolvedPath, File.GetAttributes(resolvedPath));
            var original = item.Original;
            refreshedOriginal.RenameListIndex = original.RenameListIndex;
            refreshedOriginal.InFolderIndex = original.InFolderIndex;
            refreshedOriginal.RenameListTotalCount = original.RenameListTotalCount;
            refreshedOriginal.RenameListFolderSiblingCount = original.RenameListFolderSiblingCount;
            item.Original = refreshedOriginal;
        }

        /// <summary>
        /// Resolves a stored path to the on-disk entry, including host casing, when it still exists.
        /// </summary>
        private static string? _ResolveExistingPath(string path, OnDiskCasingCache casingCache)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                return casingCache.Resolve(path);
            }

            return null;
        }

        /// <summary>
        /// Per-<see cref="RefreshOriginals"/> cache so sibling rows share parent listings and resolved paths.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Must not be reused across refresh calls: Explorer case-only renames change disk casing between F5s.
        /// Walks parents like MFR7 <c>GetFullFileName</c>.
        /// </para>
        /// </remarks>
        private sealed class OnDiskCasingCache
        {
            private readonly Dictionary<string, string> _pathToResolved = new(PathComparers.Os);
            private readonly Dictionary<string, Dictionary<string, string>> _parentToLeafName = new(PathComparers.Os);

            /// <summary>
            /// Returns <paramref name="path"/> with filesystem casing for each segment that still exists.
            /// </summary>
            /// <param name="path">Stored absolute path (any casing).</param>
            /// <returns>Path rebuilt from on-disk leaf names; unchanged segments when listing finds no match.</returns>
            public string Resolve(string path)
            {
                if (_pathToResolved.TryGetValue(path, out var cached))
                {
                    return cached;
                }

                var resolved = _ResolveUncached(path);
                _pathToResolved[path] = resolved;
                return resolved;
            }

            private string _ResolveUncached(string path)
            {
                var trimmedDirectory = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var parent = Path.GetDirectoryName(trimmedDirectory);
                var fileName = Path.GetFileName(trimmedDirectory);
                if (string.IsNullOrEmpty(fileName))
                {
                    return path;
                }

                if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                {
                    return path;
                }

                if (!_TryGetOnDiskLeafName(parent, fileName, out var onDiskLeafName))
                {
                    return path;
                }

                var resolvedParent = Resolve(parent);
                return Path.Combine(resolvedParent, onDiskLeafName);
            }

            private bool _TryGetOnDiskLeafName(string parent, string fileName, out string onDiskLeafName)
            {
                if (!_parentToLeafName.TryGetValue(parent, out var leafToCasing))
                {
                    leafToCasing = new Dictionary<string, string>(PathComparers.Os);
                    foreach (var info in new DirectoryInfo(parent).EnumerateFileSystemInfos())
                    {
                        leafToCasing[info.Name] = info.Name;
                    }

                    _parentToLeafName[parent] = leafToCasing;
                }

                return leafToCasing.TryGetValue(fileName, out onDiskLeafName!);
            }
        }

        /// <summary>
        /// Builds a rename item from a resolved filesystem path. Indices are filled after insert.
        /// </summary>
        private static RenameItem _CreateRenameItem(string fullPath, FileAttributes attrs)
        {
            return new RenameItem(_CreateOriginalSnapshot(fullPath, attrs));
        }

        /// <summary>
        /// Builds an original <see cref="FileMeta"/> snapshot from a resolved path.
        /// </summary>
        private static FileMeta _CreateOriginalSnapshot(string fullPath, FileAttributes attrs)
        {
            var isDirectory = attrs.IsDirectory();
            var (directoryPath, prefix, extension) = isDirectory
                ? _SplitRenamePathForDirectory(fullPath)
                : _SplitRenamePathForFile(fullPath);

            return new FileMeta(
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

        /// <summary>
        /// Include flags and masks for one <see cref="AddSources"/> call.
        /// </summary>
        private readonly record struct SourceResolveOptions(
            bool IncludeFiles,
            bool IncludeFolders,
            bool IncludeSubdirs,
            IReadOnlyList<string>? ExcludeMasks
        );
    }
}
