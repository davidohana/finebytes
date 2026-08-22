using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
using EngineRenameList = Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List pane: hosts the preview grid for items queued to rename.
    /// </summary>
    public sealed partial class RenameListViewModel : ViewModelBase
    {
        private readonly FileListViewModel _fileList;
        private readonly EngineRenameList _renameList = new(includeHidden: false);

        /// <summary>
        /// Initializes the Rename List and listens for File List changes that affect add commands.
        /// </summary>
        /// <param name="fileList">File List pane used as the add source.</param>
        public RenameListViewModel(FileListViewModel fileList)
        {
            ArgumentNullException.ThrowIfNull(fileList);
            _fileList = fileList;
            _fileList.PropertyChanged += _OnFileListPropertyChanged;
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
            _AddSources(RenameListAddSources.ResolveSourcesFromSelection(_fileList, ConfigStore.Config.Ui));
        }

        /// <summary>
        /// Adds every item matching the File List mask in the current folder.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddAll))]
        public void AddAll()
        {
            _AddSources(RenameListAddSources.ResolveSourcesFromCurrentFolder(_fileList, ConfigStore.Config.Ui));
        }

        private void _AddSources(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0)
            {
                return;
            }

            var ui = ConfigStore.Config.Ui;
            _renameList.AddSources(
                sources: sources,
                includeFiles: ui.AddFiles,
                includeFolders: ui.AddFolders,
                includeSubdirs: ui.AddFolderContents
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
            if (_fileList.SelectedEntries.Count == 0)
            {
                return false;
            }

            foreach (var entry in _fileList.SelectedEntries)
            {
                if (!RenameListAddSources.IsAddablePath(entry.FullPath))
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    return ConfigStore.Config.Ui.AddFiles || ConfigStore.Config.Ui.AddFolders;
                }

                if (ConfigStore.Config.Ui.AddFiles && _fileList.PassesFileMasks(entry.FullPath))
                {
                    return true;
                }
            }

            return false;
        }

        private bool _CanAddAll()
        {
            if (!_fileList.CanAddAllToCurrentFolder)
            {
                return false;
            }

            var ui = ConfigStore.Config.Ui;
            return ui.AddFiles || ui.AddFolders;
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
