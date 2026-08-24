using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Collections;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Engine.RenameList;
using Mfr.Models;
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
        private const int ProgressDialogDelayMilliseconds = 200;

        private readonly FileListViewModel _fileListViewModel;
        private readonly EngineRenameList _renameList = new(includeHidden: false);
        private readonly List<RenameListEntry> _selectedEntries = [];
        private CancellationTokenSource? _addCts;

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
        }

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
        /// Gets the most recent user-facing add failure message, or empty when none.
        /// </summary>
        [ObservableProperty]
        private string _lastAddError = string.Empty;

        /// <summary>
        /// Gets whether an add operation is in progress.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddAllCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelAddCommand))]
        private bool _isAdding;

        /// <summary>
        /// Gets whether the add-progress dialog should be shown (delayed until the add exceeds a short threshold).
        /// </summary>
        [ObservableProperty]
        private bool _isAddProgressVisible;

        /// <summary>
        /// Gets how many filesystem entries have been scanned during the current add.
        /// </summary>
        [ObservableProperty]
        private int _addScannedCount;

        /// <summary>
        /// Gets how many items have been accepted during the current add.
        /// </summary>
        [ObservableProperty]
        private int _addAddedCount;

        /// <summary>
        /// Gets the most recent path considered during the current add.
        /// </summary>
        [ObservableProperty]
        private string _addLastPath = string.Empty;

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
        }

        /// <summary>
        /// Adds the File List selection to the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelected))]
        public async Task AddSelectedAsync()
        {
            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _fileListViewModel.SelectedEntries,
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
                _fileListViewModel.Entries,
                _fileListViewModel.Mask,
                addMode
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Cancels an in-progress add and discards items from that batch.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanCancelAdd))]
        public void CancelAdd()
        {
            _addCts?.Cancel();
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
            _renameList.Remove(_selectedEntries.Select(entry => entry.EngineItem));

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (selected.Contains(Entries[i]))
                {
                    Entries.RemoveAt(i);
                }
            }

            SetSelectedEntries([]);
            _NotifyListChanged();
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
            _NotifyListChanged();
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

            _addCts = new CancellationTokenSource();
            var token = _addCts.Token;
            var progress = new Progress<RenameListAddProgress>(_OnAddProgress);

            IsAdding = true;
            IsAddProgressVisible = false;
            AddScannedCount = 0;
            AddAddedCount = 0;
            AddLastPath = string.Empty;
            LastAddError = string.Empty;

            var showProgressDelay = Task.Delay(ProgressDialogDelayMilliseconds, CancellationToken.None);
            var addTask = Task.Run(
                () =>
                    _renameList.AddSources(
                        sources: sources,
                        includeFiles: includeFiles,
                        includeFolders: includeFolders,
                        includeSubdirs: includeSubdirs,
                        excludeMasks: excludeMasks,
                        cancellationToken: token,
                        progress: progress
                    ),
                token
            );

            var completed = await Task.WhenAny(addTask, showProgressDelay).ConfigureAwait(true);
            if (completed == showProgressDelay && !addTask.IsCompleted)
            {
                IsAddProgressVisible = true;
            }

            Exception? addError = null;
            var canceled = false;
            try
            {
                await addTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Task.Run may still fault if cancel wins before the worker starts.
                canceled = true;
            }
            catch (UserException ex)
            {
                // Keep any items added before the failure; do not treat user-facing IO/validation as fatal.
                Log.Warning(ex, "Failed to add rename sources.");
                LastAddError = ex.Message;
            }
            catch (Exception ex)
            {
                addError = ex;
            }
            finally
            {
                // Engine stops the walk without throwing; treat a signaled token as user cancel.
                canceled = canceled || token.IsCancellationRequested;
                // Clear IsAdding before hiding the dialog so programmatic Close is not canceled.
                IsAdding = false;
                IsAddProgressVisible = false;
                _addCts.Dispose();
                _addCts = null;
            }

            if (canceled)
            {
                _RollbackAddedItems(oldCount);
                _NotifyListChanged();
                return;
            }

            if (addError is not null)
            {
                _RollbackAddedItems(oldCount);
                LastAddError = addError.Message;
                Log.Error(addError, "Unexpected failure while adding rename sources.");
                _NotifyListChanged();
                return;
            }

            _SyncEntriesFromEngine(oldCount);
            _NotifyListChanged();
        }

        private void _OnAddProgress(RenameListAddProgress progress)
        {
            AddScannedCount = progress.ScannedCount;
            AddAddedCount = progress.AddedCount;
            AddLastPath = progress.LastPath;
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
                _fileListViewModel.SelectedEntries,
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
            if (!RenameListAddSourceResolver.IsAddableLocation(_fileListViewModel.CurrentPath))
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _fileListViewModel.Entries,
                _fileListViewModel.Mask,
                ConfigStore.Config.Ui.AddMode
            );
        }

        private bool _CanRemoveSelected()
        {
            return !IsAdding && _selectedEntries.Count > 0;
        }

        private bool _CanClear()
        {
            return !IsAdding && Entries.Count > 0;
        }

        private bool _CanCancelAdd()
        {
            return IsAdding;
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
