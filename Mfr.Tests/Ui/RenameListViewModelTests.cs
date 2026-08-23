using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List add commands backed by the engine.
    /// </summary>
    public sealed class RenameListViewModelTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly UiConfig _originalUiConfig;

        /// <summary>
        /// Initializes config snapshot for tests that override add-policy flags.
        /// </summary>
        public RenameListViewModelTests()
        {
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            ConfigStore.Config.Ui.AddFolderContents = true;
            _originalUiConfig = _CloneUiConfig(ConfigStore.Config.Ui);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStore.Config.Ui.AddMode = _originalUiConfig.AddMode;
            ConfigStore.Config.Ui.AddFolderContents = _originalUiConfig.AddFolderContents;

            foreach (var fileListViewModel in _fileListViewModels)
            {
                fileListViewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies Add Selected adds visible file rows and ignores duplicates.
        /// </summary>
        [Fact]
        public void AddSelected_Adds_Files_And_Ignores_Duplicates()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            var alpha = _FileEntry(dir, "alpha.txt");
            var beta = _FileEntry(dir, "beta.md");
            fileListViewModel.SetSelectedEntries([alpha, beta]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));

            fileListViewModel.SetSelectedEntries([alpha]);
            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
        }

        /// <summary>
        /// Verifies the include mask hides non-matching files from Add Selected.
        /// </summary>
        [Fact]
        public void AddSelected_Honors_Include_Mask()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.Mask = "*.txt";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.DoesNotContain(fileListViewModel.Entries, entry => entry.Name == "beta.md");

            var alpha = Assert.Single(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            fileListViewModel.SetSelectedEntries([alpha]);
            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies enabled exclude masks hide matching files from Add Selected.
        /// </summary>
        [Fact]
        public void AddSelected_Honors_Exclude_Masks()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.ApplyExcludeMasks(enabled: true, editorText: "*.txt");
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.DoesNotContain(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "beta.md");

            var beta = Assert.Single(fileListViewModel.Entries, entry => entry.Name == "beta.md");
            fileListViewModel.SetSelectedEntries([beta]);
            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("beta.md", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add All adds every listed File List row without needing a selection.
        /// </summary>
        [Fact]
        public void AddAll_Adds_Listed_Masked_Files_Without_Selection()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.Mask = "*.txt";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Empty(fileListViewModel.SelectedEntries);
            renameListViewModel.AddAllCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add All is disabled on This PC (sentinel gate), even though listed places are addable.
        /// </summary>
        [Fact]
        public void AddAll_Is_Disabled_On_Computer_Path()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var fileListViewModel = _CreateFileListViewModel(_tempDirectoryFixture.CreateTempDir());
            fileListViewModel.NavigateTo(FileListViewModel.ComputerPath);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.True(fileListViewModel.Entries.Count > 0);
            Assert.False(RenameListAddSourceResolver.IsAddableLocation(fileListViewModel.CurrentPath));
            Assert.False(renameListViewModel.AddAllCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Add All is enabled on a drive root when listed child rows are addable.
        /// </summary>
        [Fact]
        public void AddAll_Is_Enabled_On_Drive_Root_With_Listed_Children()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var dir = _CreateSampleFolder();
            var root = Path.GetPathRoot(dir);
            Assert.False(string.IsNullOrEmpty(root));

            var fileListViewModel = _CreateFileListViewModel(root);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.True(RenameListAddSourceResolver.IsAddableLocation(fileListViewModel.CurrentPath));
            Assert.True(renameListViewModel.AddAllCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies preview names match originals before filter preview exists.
        /// </summary>
        [Fact]
        public void Entries_Show_Identity_Preview()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt")]);
            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileNamePreview);
            Assert.Equal("File", renameListViewModel.Entries[0].FileFolder);
        }

        /// <summary>
        /// Verifies Add Selected on a folder adds matching files recursively and no folder row (UI defaults).
        /// </summary>
        [Fact]
        public void AddSelected_Folder_Default_AddsNestedFilesNotFolder()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal(
                ["nested.mp3", "readme.txt", "track.mp3"],
                _PreviewNames(renameListViewModel).OrderBy(n => n, StringComparer.Ordinal)
            );
            Assert.DoesNotContain("album", _PreviewNames(renameListViewModel));
            Assert.DoesNotContain("disc1", _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies Add Selected on a folder with Add Folders and contents on adds nested folder rows.
        /// </summary>
        [Fact]
        public void AddSelected_Folder_AddFoldersAndContents_AddsNestedFolderRows()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.FilesAndFolders;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            var names = _PreviewNames(renameListViewModel);
            Assert.Contains("album", names);
            Assert.Contains("disc1", names);
            Assert.Contains("track.mp3", names);
            Assert.Contains("nested.mp3", names);
        }

        /// <summary>
        /// Verifies Add Selected on a folder with contents off adds the folder and its top-level files only.
        /// </summary>
        [Fact]
        public void AddSelected_Folder_ContentsOff_AddsFolderAndTopLevelFiles()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.FilesAndFolders;
            ConfigStore.Config.Ui.AddFolderContents = false;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            var names = _PreviewNames(renameListViewModel);
            Assert.Contains("album", names);
            Assert.Contains("track.mp3", names);
            Assert.Contains("readme.txt", names);
            Assert.DoesNotContain("disc1", names);
            Assert.DoesNotContain("nested.mp3", names);
        }

        /// <summary>
        /// Verifies Add All expands listed folder rows the same way as Add Selected (nested via contents).
        /// </summary>
        [Fact]
        public void AddAll_Expands_Listed_Folders_Like_AddSelected()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(albumPath);
            fileListViewModel.Mask = "*.mp3";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "disc1" && entry.IsDirectory);
            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "track.mp3");

            renameListViewModel.AddAllCommand.Execute(null);

            Assert.Equal(
                ["nested.mp3", "track.mp3"],
                _PreviewNames(renameListViewModel).OrderBy(n => n, StringComparer.Ordinal)
            );
        }

        /// <summary>
        /// Verifies Add All matches Add Selected when every listed row is selected.
        /// </summary>
        [Fact]
        public void AddAll_Matches_AddSelected_When_All_Listed_Rows_Selected()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var other = Path.Combine(parent, "other.txt");
            File.WriteAllText(other, "o");

            var addAllList = _CreateFileListViewModel(parent);
            var addAllRename = new RenameListViewModel(addAllList);
            addAllRename.AddAllCommand.Execute(null);

            var addSelectedList = _CreateFileListViewModel(parent);
            var addSelectedRename = new RenameListViewModel(addSelectedList);
            addSelectedList.SetSelectedEntries([.. addSelectedList.Entries]);
            addSelectedRename.AddSelectedCommand.Execute(null);

            Assert.Equal(
                _PreviewNames(addAllRename).OrderBy(n => n, StringComparer.Ordinal),
                _PreviewNames(addSelectedRename).OrderBy(n => n, StringComparer.Ordinal)
            );
        }

        /// <summary>
        /// Verifies Add Selected can mix an exact file with a folder source.
        /// </summary>
        [Fact]
        public void AddSelected_MixedFileAndFolder_AddsBoth()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var other = Path.Combine(parent, "other.txt");
            File.WriteAllText(other, "o");
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FileEntry(parent, "other.txt"), _FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            var names = _PreviewNames(renameListViewModel);
            Assert.Contains("other.txt", names);
            Assert.Contains("track.mp3", names);
        }

        /// <summary>
        /// Verifies Add Selected with files off and folders on adds the selected folder and descendant folders.
        /// </summary>
        [Fact]
        public void AddSelected_Folder_FilesOffFoldersOn_AddsFolderAndDescendants()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Folders;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal(
                ["album", "disc1"],
                _PreviewNames(renameListViewModel).OrderBy(n => n, StringComparer.Ordinal)
            );
        }

        /// <summary>
        /// Verifies Add Selected CanExecute for folder vs file selection under each add mode.
        /// </summary>
        [Fact]
        public void AddSelected_CanExecute_DependsOnAddModeAndSelection()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(parent);
            var folderEntry = _FolderEntry(albumPath);
            var fileEntry = _FileEntry(parent, "other.txt");
            File.WriteAllText(fileEntry.FullPath, "o");
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([folderEntry]);
            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));

            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Folders;
            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));

            fileListViewModel.SetSelectedEntries([fileEntry]);
            Assert.False(renameListViewModel.AddSelectedCommand.CanExecute(null));

            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Remove Selected drops selected rows and leaves the rest.
        /// </summary>
        [Fact]
        public void RemoveSelected_Removes_Only_Selected_Rows()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            renameListViewModel.AddSelectedCommand.Execute(null);
            Assert.Equal(2, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.RemoveSelectedCommand.CanExecute(null));

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);
            Assert.True(renameListViewModel.RemoveSelectedCommand.CanExecute(null));

            renameListViewModel.RemoveSelectedCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("beta.md", renameListViewModel.Entries[0].FullFileName);
            Assert.Empty(renameListViewModel.SelectedEntries);
            Assert.Equal(1, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.RemoveSelectedCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Clear empties the list and updates ItemCount / CanExecute.
        /// </summary>
        [Fact]
        public void Clear_Removes_All_Rows()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.False(renameListViewModel.ClearCommand.CanExecute(null));

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            renameListViewModel.AddSelectedCommand.Execute(null);
            Assert.True(renameListViewModel.ClearCommand.CanExecute(null));
            Assert.Equal(2, renameListViewModel.ItemCount);

            renameListViewModel.ClearCommand.Execute(null);

            Assert.Empty(renameListViewModel.Entries);
            Assert.Equal(0, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.ClearCommand.CanExecute(null));
        }

        private string _CreateSampleFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private (string parent, string albumPath) _CreateAlbumTree()
        {
            var parent = _tempDirectoryFixture.CreateTempDir();
            var albumPath = Path.Combine(parent, "album");
            Directory.CreateDirectory(Path.Combine(albumPath, "disc1"));
            File.WriteAllText(Path.Combine(albumPath, "track.mp3"), "t");
            File.WriteAllText(Path.Combine(albumPath, "readme.txt"), "r");
            File.WriteAllText(Path.Combine(albumPath, "disc1", "nested.mp3"), "n");
            return (parent, albumPath);
        }

        private FileListViewModel _CreateFileListViewModel(string path)
        {
            var fileListViewModel = new FileListViewModel(NullSystemIconProvider.Instance, path);
            _fileListViewModels.Add(fileListViewModel);
            return fileListViewModel;
        }

        private static FileListEntry _FolderEntry(string directoryPath)
        {
            return new FileListEntry
            {
                Name = Path.GetFileName(directoryPath),
                FullPath = directoryPath,
                IsDirectory = true,
            };
        }

        private static FileListEntry _FileEntry(string directory, string fileName)
        {
            return new FileListEntry
            {
                Name = fileName,
                FullPath = Path.Combine(directory, fileName),
                IsDirectory = false,
            };
        }

        private static IReadOnlyList<string> _PreviewNames(RenameListViewModel renameListViewModel)
        {
            return [.. renameListViewModel.Entries.Select(entry => entry.FullFileName)];
        }

        private static UiConfig _CloneUiConfig(UiConfig source)
        {
            return new UiConfig
            {
                AddMode = source.AddMode,
                AddFolderContents = source.AddFolderContents,
                RememberWindowState = source.RememberWindowState,
                RememberLastFolder = source.RememberLastFolder,
            };
        }
    }
}
