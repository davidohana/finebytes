using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Collections;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Engine.RenameList;
using Mfr.Models.Config;
using Mfr.Models.Rename;
using Serilog;
using EngineRenameList = Mfr.Engine.RenameList.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List pane: hosts the preview grid for items queued to rename.
    /// </summary>
    public sealed partial class RenameListViewModel : ViewModelBase
    {
        private readonly FileListViewModel _fileListViewModel;
        private readonly EngineRenameList _renameList = new(includeHidden: false);
        private readonly List<RenameListEntry> _selectedEntries = [];
        private List<RenameListSortKey> _sortKeys = [];

        /// <summary>
        /// Initializes the Rename List and listens for File List changes that affect add commands.
        /// </summary>
        /// <param name="fileListViewModel">File List pane used as the add source.</param>
        public RenameListViewModel(FileListViewModel fileListViewModel)
        {
            ArgumentNullException.ThrowIfNull(fileListViewModel);
            _fileListViewModel = fileListViewModel;
            _fileListViewModel.PropertyChanged += _OnFileListPropertyChanged;
            _fileListViewModel.Entries.CollectionChanged += _OnFileListEntriesChanged;
            AddProgress.PropertyChanged += _OnAddProgressPropertyChanged;
        }

        /// <summary>
        /// Gets progress, cancel, and delayed-dialog state for the current add.
        /// </summary>
        public RenameListAddProgressViewModel AddProgress { get; } = new();

        /// <summary>
        /// Gets the rows shown in the Rename List grid.
        /// </summary>
        /// <para>
        /// A <see cref="RangeObservableCollection{T}"/> so large adds can append thousands of new rows with
        /// one grid refresh instead of one per row.
        /// </para>
        public RangeObservableCollection<RenameListEntry> Entries { get; } = [];

        /// <summary>
        /// Gets the currently selected Rename List rows.
        /// </summary>
        public IReadOnlyList<RenameListEntry> SelectedEntries => _selectedEntries;

        /// <summary>
        /// Gets the row index under a file or internal drag (insert-before target), or null when unset.
        /// </summary>
        public int? DropMarkIndex { get; private set; }

        /// <summary>
        /// Gets the count of items in the Rename List.
        /// </summary>
        public int ItemCount => Entries.Count;

        /// <summary>
        /// Gets whether an add operation is in progress.
        /// </summary>
        public bool IsAdding => AddProgress.IsAdding;

        /// <summary>
        /// Gets whether Auto-Sort is active (one or more sort keys).
        /// </summary>
        public bool IsAutoSort => _sortKeys.Count > 0;

        /// <summary>
        /// Gets the active Auto-Sort keys (empty when Auto-Sort is off).
        /// </summary>
        public IReadOnlyList<RenameListSortKey> SortKeys => _sortKeys;

        /// <summary>
        /// Auto-Sort tooltip: active keys when on, or prompt to enable with default keys when off.
        /// </summary>
        public string SortSummaryText => RenameListSortDisplay.FormatSummary(_sortKeys);

        /// <summary>
        /// Sort priority and direction glyphs keyed by column for Rename List headers.
        /// </summary>
        public RenameListColumnSortStates ColumnSortStates { get; private set; } = RenameListColumnSortStates.Inactive;

        /// <summary>
        /// Gets the most recent user-facing add failure message, or empty when none.
        /// </summary>
        [ObservableProperty]
        private string _lastAddError = string.Empty;

        /// <summary>
        /// Gets the most recent locate-in-File-List failure message, or empty when none.
        /// </summary>
        [ObservableProperty]
        private string _lastLocateError = string.Empty;

        /// <summary>
        /// Status-bar hint for the focused or selected Rename List cell.
        /// </summary>
        [ObservableProperty]
        private StatusHintDisplay _cellStatusHintDisplay = StatusHintDisplay.Empty;

        /// <summary>
        /// Replaces the Rename List selection.
        /// </summary>
        /// <param name="entries">Selected grid rows.</param>
        public void SetSelectedEntries(IReadOnlyList<RenameListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var entrySet = Entries.ToHashSet();
            _selectedEntries.Clear();
            foreach (var entry in entries)
            {
                if (entrySet.Contains(entry))
                {
                    _selectedEntries.Add(entry);
                }
            }

            OnPropertyChanged(nameof(SelectedEntries));
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            LocateInFileListCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Sets or clears the drag insert marker (row index to insert before).
        /// </summary>
        /// <param name="index">Zero-based row index under the pointer, or null to clear.</param>
        /// <remarks>
        /// <para>
        /// Ignored while Auto-Sort is on (MFR7: external drops append and resort; no mark).
        /// </para>
        /// </remarks>
        public void SetDropMarkIndex(int? index)
        {
            if (IsAutoSort)
            {
                index = null;
            }

            if (index is { } i && (i < 0 || i >= Entries.Count))
            {
                index = null;
            }

            if (DropMarkIndex == index)
            {
                return;
            }

            DropMarkIndex = index;
            OnPropertyChanged(nameof(DropMarkIndex));
        }

        /// <summary>
        /// Adds the File List selection to the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelected))]
        public async Task AddSelectedAsync()
        {
            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                addMode
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Adds every listed File List row to the Rename List (same rules as Add Selected).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddAll))]
        public async Task AddAllAsync()
        {
            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _ToSourceItems(_fileListViewModel.Entries),
                _fileListViewModel.Mask,
                addMode
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Adds dropped filesystem paths to the Rename List using the same rules as Add Selected.
        /// </summary>
        /// <param name="paths">Full file or folder paths from File List or Explorer drag-drop.</param>
        public async Task AddPathsAsync(IReadOnlyList<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromPaths(paths, _fileListViewModel.Mask, addMode);
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Removes the selected Rename List rows.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void RemoveSelected()
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var selected = _selectedEntries.ToHashSet();
            var anchorIndex = _FindFirstSelectedIndex(selected);
            _renameList.Remove(_selectedEntries.Select(entry => entry.EngineItem));

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (selected.Contains(Entries[i]))
                {
                    Entries.RemoveAt(i);
                }
            }

            var nextSelection = _SelectEntryAfterRemove(anchorIndex);
            SetSelectedEntries(nextSelection is null ? [] : [nextSelection]);
            _NotifyListChanged();
        }

        /// <summary>
        /// Removes every Rename List row that is not selected, keeping the selection.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveAllButSelected))]
        public void RemoveAllButSelected()
        {
            if (_selectedEntries.Count == 0 || _selectedEntries.Count >= Entries.Count)
            {
                return;
            }

            var selected = _selectedEntries.ToHashSet();
            var toRemove = Entries.Where(entry => !selected.Contains(entry)).Select(entry => entry.EngineItem).ToList();
            _renameList.Remove(toRemove);

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (!selected.Contains(Entries[i]))
                {
                    Entries.RemoveAt(i);
                }
            }

            SetSelectedEntries([.. Entries.Where(selected.Contains)]);
            _NotifyListChanged();
        }

        /// <summary>
        /// Finds the list index of the first selected row, matching MFR7 remove behavior.
        /// </summary>
        private int _FindFirstSelectedIndex(IReadOnlyCollection<RenameListEntry> selected)
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                if (selected.Contains(Entries[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Picks the row to focus after delete: same index when possible, otherwise the last row.
        /// </summary>
        private RenameListEntry? _SelectEntryAfterRemove(int anchorIndex)
        {
            if (Entries.Count == 0 || anchorIndex < 0)
            {
                return null;
            }

            var nextIndex = Math.Min(anchorIndex, Entries.Count - 1);
            return Entries[nextIndex];
        }

        /// <summary>
        /// Removes every row from the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanClear))]
        public void Clear()
        {
            if (Entries.Count == 0)
            {
                return;
            }

            _renameList.Clear();
            Entries.Clear();
            SetDropMarkIndex(null);
            SetSelectedEntries([]);
            CellStatusHintDisplay = StatusHintDisplay.Empty;
            _NotifyListChanged();
        }

        /// <summary>
        /// Moves the selected Rename List rows one position up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void MoveSelectedUp()
        {
            _MoveSelected(offset: -1);
        }

        /// <summary>
        /// Moves the selected Rename List rows one position down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void MoveSelectedDown()
        {
            _MoveSelected(offset: 1);
        }

        /// <summary>
        /// Turns off Auto-Sort for a manual reorder (drag or keyboard move) without resorting.
        /// </summary>
        public void CancelAutoSort()
        {
            if (!IsAutoSort)
            {
                return;
            }

            _SetSortKeys([], resort: false);
        }

        /// <summary>
        /// Toggles Auto-Sort. Turning it on restores the default keys.
        /// </summary>
        [RelayCommand]
        public void ToggleAutoSort()
        {
            if (IsAutoSort)
            {
                _SetSortKeys([], resort: false);
                return;
            }

            _SetSortKeys(RenameListSortKey.DefaultKeys, resort: true);
        }

        /// <summary>
        /// Sets Auto-Sort to a single original column (header click), inverting when already ascending on that column.
        /// </summary>
        /// <param name="memberPath">
        /// <see cref="RenameListEntry"/> property name (<c>FileFolder</c>, <c>ParentFolder</c>, <c>FullFileName</c>).
        /// </param>
        public void SortByColumn(string? memberPath)
        {
            if (!_TryMapSortMemberPath(memberPath, out var column))
            {
                return;
            }

            // MFR7 RenameGridCells.SetSortMode: desc = GetSortMode() == Ascending (None → ascending).
            var descending = _sortKeys.Any(key => key.Column == column && !key.Descending);
            _SetSortKeys([new RenameListSortKey(column, descending)], resort: true);
        }

        /// <summary>
        /// Reorders the selection to insert before the drop mark (or appends when unset).
        /// </summary>
        /// <returns><see langword="true"/> when the list order changed.</returns>
        /// <remarks>
        /// <para>
        /// Dropping onto a marked row that is part of the selection is a no-op (MFR7). Clears the
        /// drop mark afterward. Cancels Auto-Sort (MFR7 manual reorder).
        /// </para>
        /// </remarks>
        public bool ReorderSelectedToDropMark()
        {
            try
            {
                if (IsAdding || _selectedEntries.Count == 0)
                {
                    return false;
                }

                var beforeItem = DropMarkIndex is { } markIndex ? Entries[markIndex].EngineItem : null;
                var engineItems = _selectedEntries.Select(entry => entry.EngineItem);
                if (!_renameList.MoveSelectedBefore(engineItems, beforeItem: beforeItem))
                {
                    return false;
                }

                CancelAutoSort();

                _SyncEntriesToEngineOrder();
                return true;
            }
            finally
            {
                SetDropMarkIndex(null);
            }
        }

        /// <summary>
        /// Reorders selected rows in the engine and grid by one step.
        /// </summary>
        private void _MoveSelected(int offset)
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var engineItems = _selectedEntries.Select(entry => entry.EngineItem);
            if (!_renameList.MoveSelected(engineItems, offset))
            {
                return;
            }

            CancelAutoSort();

            _SyncEntriesToEngineOrder();
        }

        /// <summary>
        /// Rebuilds <see cref="Entries"/> to match engine order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// DataGrid ignores Move; ReplaceAll raises Reset so the grid refreshes.
        /// </para>
        /// </remarks>
        private void _SyncEntriesToEngineOrder()
        {
            var engineItemToEntry = Entries.ToDictionary(entry => entry.EngineItem);
            Entries.ReplaceAll(_renameList.RenameItems.Select(item => engineItemToEntry[item]));
            SetSelectedEntries([.. _selectedEntries]);
        }

        /// <summary>
        /// Navigates the File List to the focused Rename List row and selects it there.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanLocateInFileList))]
        public void LocateInFileList()
        {
            var entry = _GetFocusedSelectedEntry();
            if (entry is null)
            {
                return;
            }

            var fullPath = entry.EngineItem.Original.FullPath;
            if (_fileListViewModel.TryLocatePath(fullPath))
            {
                LastLocateError = string.Empty;
                return;
            }

            LastLocateError = $"Failed to locate \"{fullPath}\" in the File List.";
        }

        /// <summary>
        /// Resolves sources into the engine, then mirrors a successful commit into <see cref="Entries"/>.
        /// </summary>
        private async Task _AddSourcesAsync(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0 || IsAdding)
            {
                return;
            }

            var autoSort = IsAutoSort;
            var usedDropMark = !autoSort && DropMarkIndex is not null;
            var selectFirstAdded = !autoSort && (usedDropMark || _selectedEntries.Count > 0);
            var insertAt = _ResolveInsertAt();
            SetDropMarkIndex(null);
            var oldCount = _renameList.RenameItems.Count;
            LastAddError = string.Empty;

            var ui = ConfigStore.Config.Ui;
            var excludeMasks = _fileListViewModel.ExcludeMasksEnabled ? _fileListViewModel.ExcludeMasks : null;
            var addSummary = new RenameListAddSummary(0);
            var completed = false;
            try
            {
                completed = await AddProgress
                    .RunAsync(
                        (token, progress) =>
                            addSummary = _renameList.AddSources(
                                sources: sources,
                                includeFiles: ui.AddMode.IncludesFiles(),
                                includeFolders: ui.AddMode.IncludesFolders(),
                                includeSubdirs: ui.AddFolderContents,
                                excludeMasks: excludeMasks,
                                cancellationToken: token,
                                progress: progress,
                                insertAtIndex: insertAt
                            )
                    )
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LastAddError = ex.Message;
                Log.Error(ex, "Unexpected failure while adding rename sources.");
            }

            if (!completed)
            {
                _RollbackAddedItems(insertAt, oldCount);
                _NotifyListChanged();
                return;
            }

            _SyncEntriesAfterAdd(insertAt, oldCount);
            if (autoSort)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }

            var addedCount = _renameList.RenameItems.Count - oldCount;
            if (selectFirstAdded && addedCount > 0)
            {
                SetSelectedEntries([Entries[insertAt]]);
            }

            LastAddError = _FormatAddOutcome(addedCount: addedCount, skippedSourceCount: addSummary.SkippedSourceCount);
            _LogAddOutcome(
                addedCount: addedCount,
                skippedSourceCount: addSummary.SkippedSourceCount,
                sourceCount: sources.Count
            );
            _NotifyListChanged();
        }

        /// <summary>
        /// Index to insert new rows: append when Auto-Sort; else before drop mark / after first selected / append.
        /// </summary>
        private int _ResolveInsertAt()
        {
            if (IsAutoSort)
            {
                return Entries.Count;
            }

            if (DropMarkIndex is { } markIndex)
            {
                return markIndex;
            }

            if (_selectedEntries.Count == 0)
            {
                return Entries.Count;
            }

            return _FindFirstSelectedIndex(_selectedEntries) + 1;
        }

        /// <summary>
        /// Builds the status-bar message after a completed add.
        /// </summary>
        private static string _FormatAddOutcome(int addedCount, int skippedSourceCount)
        {
            if (skippedSourceCount > 0)
            {
                return $"Added {addedCount} item(s). Skipped {skippedSourceCount} inaccessible source(s).";
            }

            if (addedCount == 0)
            {
                return "No items were added.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Writes a batch summary when the add had skips or added nothing.
        /// </summary>
        private static void _LogAddOutcome(int addedCount, int skippedSourceCount, int sourceCount)
        {
            if (skippedSourceCount > 0)
            {
                Log.Warning(
                    "Rename list add finished. Added {AddedCount} item(s) from {SourceCount} source(s). Skipped {SkippedSourceCount} inaccessible source(s).",
                    addedCount,
                    sourceCount,
                    skippedSourceCount
                );
                return;
            }

            if (addedCount == 0)
            {
                Log.Warning("Rename list add finished with no items added from {SourceCount} source(s).", sourceCount);
            }
        }

        /// <summary>
        /// Mirrors a committed engine add into <see cref="Entries"/>.
        /// </summary>
        /// <param name="insertAt">Index where the engine inserted the new items.</param>
        /// <param name="oldCount">Engine list size before this add; used to count new rows.</param>
        /// <remarks>
        /// <para>
        /// Called only after a successful <c>AddSources</c> that committed its staging batch. Creates
        /// UI rows for the newly inserted engine items at the same index.
        /// </para>
        /// </remarks>
        private void _SyncEntriesAfterAdd(int insertAt, int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            var addedCount = renameItems.Count - oldCount;
            if (addedCount <= 0)
            {
                return;
            }

            var newEntries = new List<RenameListEntry>(addedCount);
            for (var i = insertAt; i < insertAt + addedCount; i++)
            {
                newEntries.Add(RenameListEntryMapper.ToEntry(renameItems[i]));
            }

            Entries.InsertRange(insertAt, newEntries);
        }

        /// <summary>
        /// Undoes an engine commit if cancel raced after the staging batch was inserted.
        /// </summary>
        /// <param name="insertAt">Index where the engine inserted the new items.</param>
        /// <param name="oldCount">Engine list size before this add; used to count new rows.</param>
        /// <remarks>
        /// <para>
        /// Usually a no-op: cancel discards the staging batch before it reaches <c>RenameItems</c>.
        /// This only runs when the walk finished and inserted, but the UI still saw cancel/failure.
        /// </para>
        /// </remarks>
        private void _RollbackAddedItems(int insertAt, int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            var addedCount = renameItems.Count - oldCount;
            if (addedCount <= 0)
            {
                return;
            }

            _renameList.Remove([.. renameItems.Skip(insertAt).Take(addedCount)]);
        }

        private void _NotifyListChanged()
        {
            OnPropertyChanged(nameof(ItemCount));
            ClearCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Restores Auto-Sort keys from a session value.
        /// </summary>
        /// <param name="sortFields">
        /// Session sort fields, empty to disable Auto-Sort, or <see langword="null"/> for the default keys.
        /// </param>
        internal void ApplySession(IReadOnlyList<SessionStateRenameListSortField>? sortFields)
        {
            var keys = sortFields is null
                ? RenameListSortKey.DefaultKeys
                : SessionStateRenameList.ToSortKeys(sortFields);
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Captures the current Auto-Sort keys for session save.
        /// </summary>
        /// <returns>Session sort fields, or empty when Auto-Sort is off.</returns>
        internal IReadOnlyList<SessionStateRenameListSortField> CaptureSortFields()
        {
            return SessionStateRenameList.FromSortKeys(_sortKeys);
        }

        private void _SetSortKeys(IReadOnlyList<RenameListSortKey> keys, bool resort)
        {
            _sortKeys = [.. keys];
            ColumnSortStates = RenameListSortDisplay.BuildColumnSortStates(_sortKeys);
            OnPropertyChanged(nameof(IsAutoSort));
            OnPropertyChanged(nameof(SortKeys));
            OnPropertyChanged(nameof(SortSummaryText));
            OnPropertyChanged(nameof(ColumnSortStates));
            SetDropMarkIndex(null);

            if (resort && IsAutoSort && Entries.Count > 1)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }
        }

        private static bool _TryMapSortMemberPath(string? memberPath, out RenameListSortColumn column)
        {
            switch (memberPath)
            {
                case nameof(RenameListEntry.FileFolder):
                    column = RenameListSortColumn.FileFolder;
                    return true;
                case nameof(RenameListEntry.ParentFolder):
                    column = RenameListSortColumn.ParentFolder;
                    return true;
                case nameof(RenameListEntry.FullFileName):
                    column = RenameListSortColumn.FullFileName;
                    return true;
                default:
                    column = default;
                    return false;
            }
        }

        private bool _CanAddSelected()
        {
            if (IsAdding)
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                ConfigStore.Config.Ui.AddMode
            );
        }

        private bool _CanAddAll()
        {
            if (IsAdding)
            {
                return false;
            }

            // Sentinel gate: This PC / Network list Known Places and volumes; Add All would mass-add those.
            // Drive roots are fine — Add All walks listed children, not the root path itself.
            if (!RenameListAddSourceResolver.CanAddAllFrom(_fileListViewModel.CurrentPath))
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _ToSourceItems(_fileListViewModel.Entries),
                _fileListViewModel.Mask,
                ConfigStore.Config.Ui.AddMode
            );
        }

        private static IReadOnlyList<FileListSourceItem> _ToSourceItems(IEnumerable<FileListEntry> entries)
        {
            return [.. entries.Select(entry => new FileListSourceItem(entry.FullPath, entry.IsDirectory))];
        }

        private bool _CanRemoveSelected()
        {
            return !IsAdding && _selectedEntries.Count > 0;
        }

        private bool _CanRemoveAllButSelected()
        {
            return !IsAdding && _selectedEntries.Count > 0 && _selectedEntries.Count < Entries.Count;
        }

        private bool _CanClear()
        {
            return !IsAdding && Entries.Count > 0;
        }

        private bool _CanLocateInFileList()
        {
            return !IsAdding && _GetFocusedSelectedEntry() is not null;
        }

        private RenameListEntry? _GetFocusedSelectedEntry()
        {
            return _selectedEntries.Count > 0 ? _selectedEntries[^1] : null;
        }

        private void _OnAddProgressPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not nameof(RenameListAddProgressViewModel.IsAdding))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAdding));
            AddSelectedCommand.NotifyCanExecuteChanged();
            AddAllCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            LocateInFileListCommand.NotifyCanExecuteChanged();
        }

        private void _OnFileListPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_AffectsAddCommands(e.PropertyName))
            {
                return;
            }

            AddSelectedCommand.NotifyCanExecuteChanged();
            AddAllCommand.NotifyCanExecuteChanged();
        }

        private void _OnFileListEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            AddAllCommand.NotifyCanExecuteChanged();
        }

        private static bool _AffectsAddCommands(string? propertyName)
        {
            return propertyName
                is nameof(FileListViewModel.SelectedEntry)
                    or nameof(FileListViewModel.SelectedEntries)
                    or nameof(FileListViewModel.CurrentPath)
                    or nameof(FileListViewModel.Mask)
                    or nameof(FileListViewModel.ExcludeMasksEnabled)
                    or nameof(FileListViewModel.ExcludeMasks);
        }
    }
}
