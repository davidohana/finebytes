using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
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
        /// Replaces the Rename List selection.
        /// </summary>
        /// <param name="entries">Selected grid rows.</param>
        public void SetSelectedEntries(IReadOnlyList<RenameListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            _selectedEntries.Clear();
            foreach (var entry in entries)
            {
                if (Entries.Contains(entry))
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
        /// Adds every item matching the File List mask in the current folder.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddAll))]
        public void AddAll()
        {
            var sources = RenameListAddSourceResolver.ResolveSourcesFromCurrentFolder(
                _fileListViewModel.CurrentPath,
                _fileListViewModel.Mask
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

            _renameList.Remove(_selectedEntries.Select(entry => entry.EngineItem));
            _SyncEntries();
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
            _SyncEntries();
        }

        private void _AddSources(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0)
            {
                return;
            }

            var uiConfig = ConfigStore.Config.Ui;
            var excludeMasks = _fileListViewModel.ExcludeMasksEnabled ? _fileListViewModel.ExcludeMasks : null;
            _renameList.AddSources(
                sources: sources,
                includeFiles: uiConfig.AddMode.IncludesFiles(),
                includeFolders: uiConfig.AddMode.IncludesFolders(),
                includeSubdirs: uiConfig.AddFolderContents,
                excludeMasks: excludeMasks
            );
            _SyncEntries();
        }

        private void _SyncEntries()
        {
            Entries.Clear();
            foreach (var item in _renameList.RenameItems)
            {
                Entries.Add(RenameListEntryMapper.ToEntry(item));
            }

            SetSelectedEntries([]);
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
            return RenameListAddSourceResolver.CanResolveFromCurrentFolder(_fileListViewModel.CurrentPath);
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
