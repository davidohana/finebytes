using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
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
        }

        /// <summary>
        /// Gets the rows shown in the Rename List grid.
        /// </summary>
        public ObservableCollection<RenameListEntry> Entries { get; } = [];

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
        public void AddSelected()
        {
            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _fileListViewModel.SelectedEntries,
                _fileListViewModel.Mask,
                addMode
            );
            _AddSources(sources);
        }

        /// <summary>
        /// Adds every listed File List row to the Rename List (same rules as Add Selected).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddAll))]
        public void AddAll()
        {
            var addMode = ConfigStore.Config.Ui.AddMode;
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _fileListViewModel.Entries,
                _fileListViewModel.Mask,
                addMode
            );
            _AddSources(sources);
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

        private void _AddSources(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0)
            {
                return;
            }

            var oldCount = _renameList.RenameItems.Count;
            var uiConfig = ConfigStore.Config.Ui;
            var excludeMasks = _fileListViewModel.ExcludeMasksEnabled ? _fileListViewModel.ExcludeMasks : null;
            try
            {
                LastAddError = string.Empty;
                _renameList.AddSources(
                    sources: sources,
                    includeFiles: uiConfig.AddMode.IncludesFiles(),
                    includeFolders: uiConfig.AddMode.IncludesFolders(),
                    includeSubdirs: uiConfig.AddFolderContents,
                    excludeMasks: excludeMasks
                );
            }
            catch (UserException ex)
            {
                // Keep any items added before the failure; do not treat user-facing IO/validation as fatal.
                Log.Warning(ex, "Failed to add rename sources.");
                LastAddError = ex.Message;
            }

            var renameItems = _renameList.RenameItems;
            for (var i = oldCount; i < renameItems.Count; i++)
            {
                Entries.Add(RenameListEntryMapper.ToEntry(renameItems[i]));
            }

            _NotifyListChanged();
        }

        private void _NotifyListChanged()
        {
            OnPropertyChanged(nameof(ItemCount));
            ClearCommand.NotifyCanExecuteChanged();
        }

        private bool _CanAddSelected()
        {
            return RenameListAddSourceResolver.CanResolveFromSelection(
                _fileListViewModel.SelectedEntries,
                _fileListViewModel.Mask,
                ConfigStore.Config.Ui.AddMode
            );
        }

        private bool _CanAddAll()
        {
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
            return _selectedEntries.Count > 0;
        }

        private bool _CanClear()
        {
            return Entries.Count > 0;
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
