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
