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
        /// Gets the count of items in the Rename List.
        /// </summary>
        public int ItemCount => Entries.Count;

        /// <summary>
        /// Gets whether an add operation is in progress.
        /// </summary>
        public bool IsAdding => AddProgress.IsAdding;

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
        private int _FindFirstSelectedIndex(HashSet<RenameListEntry> selected)
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

            // DataGrid ignores Move; ReplaceAll raises Reset so the grid refreshes.
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

        private async Task _AddSourcesAsync(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0 || IsAdding)
            {
                return;
            }

            var (insertAt, selectFirstAdded) = _ResolveInsertAt();
            var oldCount = _renameList.RenameItems.Count;
            var uiConfig = ConfigStore.Config.Ui;
            var includeFiles = uiConfig.AddMode.IncludesFiles();
            var includeFolders = uiConfig.AddMode.IncludesFolders();
            var includeSubdirs = uiConfig.AddFolderContents;
            var excludeMasks = _fileListViewModel.ExcludeMasksEnabled ? _fileListViewModel.ExcludeMasks : null;
            LastAddError = string.Empty;

            var addSummary = new RenameListAddSummary(0);
            bool completed;
            try
            {
                completed = await AddProgress
                    .RunAsync(
                        (token, progress) =>
                            addSummary = _renameList.AddSources(
                                sources: sources,
                                includeFiles: includeFiles,
                                includeFolders: includeFolders,
                                includeSubdirs: includeSubdirs,
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
                _RollbackAddedItems(insertAt, oldCount);
                LastAddError = ex.Message;
                Log.Error(ex, "Unexpected failure while adding rename sources.");
                _NotifyListChanged();
                return;
            }

            if (!completed)
            {
                _RollbackAddedItems(insertAt, oldCount);
                _NotifyListChanged();
                return;
            }

            _SyncEntriesAfterAdd(insertAt, oldCount);
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
        /// Chooses where new rows go in manual mode (MFR7 help Manual Sort).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Until Auto-Sort exists, the list is always manual: insert after the first selected row and
        /// select the first newly added row; with no selection, append and leave selection unchanged.
        /// Matches MFR7 help (“below the selected item”); legacy source inserted before.
        /// </para>
        /// </remarks>
        private (int InsertAt, bool SelectFirstAdded) _ResolveInsertAt()
        {
            if (_selectedEntries.Count == 0)
            {
                return (Entries.Count, false);
            }

            var selected = _selectedEntries.ToHashSet();
            var firstSelectedIndex = _FindFirstSelectedIndex(selected);
            if (firstSelectedIndex < 0)
            {
                return (Entries.Count, false);
            }

            return (firstSelectedIndex + 1, true);
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
        /// Syncs <see cref="Entries"/> with engine items added by the latest batch.
        /// </summary>
        /// <param name="insertAt">Index where the batch was inserted.</param>
        /// <param name="oldCount">Rename List size before the add started.</param>
        /// <remarks>
        /// <para>
        /// Appends when the batch landed at the end; for mid-list inserts, rebuilds row order while
        /// keeping existing <see cref="RenameListEntry"/> object identity.
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

            if (insertAt >= oldCount)
            {
                var newEntries = new List<RenameListEntry>(addedCount);
                for (var i = insertAt; i < insertAt + addedCount; i++)
                {
                    newEntries.Add(RenameListEntryMapper.ToEntry(renameItems[i]));
                }

                Entries.AddRange(newEntries);
                return;
            }

            // Mid-list insert: rebuild order while keeping existing entry object identity.
            var engineItemToEntry = Entries.ToDictionary(entry => entry.EngineItem);
            var rebuilt = new List<RenameListEntry>(renameItems.Count);
            foreach (var item in renameItems)
            {
                if (engineItemToEntry.TryGetValue(item, out var existing))
                {
                    rebuilt.Add(existing);
                }
                else
                {
                    rebuilt.Add(RenameListEntryMapper.ToEntry(item));
                }
            }

            Entries.ReplaceAll(rebuilt);
        }

        /// <summary>
        /// Removes items added by a canceled or failed add, including mid-list inserts.
        /// </summary>
        /// <param name="insertAt">Index where the batch was inserted.</param>
        /// <param name="oldCount">Rename List size before the add started.</param>
        /// <remarks>
        /// <para>
        /// Engine rows from <paramref name="insertAt"/> for <c>Count - oldCount</c> items are removed.
        /// <see cref="Entries"/> is trimmed only if it was already synced (normally it is not until success).
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

            var toRemove = renameItems.Skip(insertAt).Take(addedCount).ToList();
            _renameList.Remove(toRemove);

            // Entries are only synced after a successful add; trim is defensive.
            if (Entries.Count <= oldCount)
            {
                return;
            }

            if (insertAt >= oldCount)
            {
                while (Entries.Count > oldCount)
                {
                    Entries.RemoveAt(Entries.Count - 1);
                }

                return;
            }

            for (var i = insertAt + addedCount - 1; i >= insertAt; i--)
            {
                if (i < Entries.Count)
                {
                    Entries.RemoveAt(i);
                }
            }
        }

        private void _NotifyListChanged()
        {
            OnPropertyChanged(nameof(ItemCount));
            ClearCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
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
