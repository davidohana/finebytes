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
        /// Status-bar hint for the Rename List cell under the pointer or keyboard focus.
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
                                progress: progress
                            )
                    )
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _RollbackAddedItems(oldCount);
                LastAddError = ex.Message;
                Log.Error(ex, "Unexpected failure while adding rename sources.");
                _NotifyListChanged();
                return;
            }

            if (!completed)
            {
                _RollbackAddedItems(oldCount);
                _NotifyListChanged();
                return;
            }

            _SyncEntriesFromEngine(oldCount);
            var addedCount = _renameList.RenameItems.Count - oldCount;
            LastAddError = _FormatAddOutcome(addedCount: addedCount, skippedSourceCount: addSummary.SkippedSourceCount);
            _LogAddOutcome(
                addedCount: addedCount,
                skippedSourceCount: addSummary.SkippedSourceCount,
                sourceCount: sources.Count
            );
            _NotifyListChanged();
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

        private void _SyncEntriesFromEngine(int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            if (renameItems.Count <= oldCount)
            {
                return;
            }

            var newEntries = new List<RenameListEntry>(renameItems.Count - oldCount);
            for (var i = oldCount; i < renameItems.Count; i++)
            {
                newEntries.Add(RenameListEntryMapper.ToEntry(renameItems[i]));
            }

            Entries.AddRange(newEntries);
        }

        private void _RollbackAddedItems(int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            if (renameItems.Count <= oldCount)
            {
                return;
            }

            var toRemove = renameItems.Skip(oldCount).ToList();
            _renameList.Remove(toRemove);

            while (Entries.Count > oldCount)
            {
                Entries.RemoveAt(Entries.Count - 1);
            }
        }

        private void _NotifyListChanged()
        {
            OnPropertyChanged(nameof(ItemCount));
            ClearCommand.NotifyCanExecuteChanged();
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
            ClearCommand.NotifyCanExecuteChanged();
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
