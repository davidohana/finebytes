using Mfr.App.Ui.Services.FileList;
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
            ConfigStore.Config.Ui.AddFiles = true;
            ConfigStore.Config.Ui.AddFolders = false;
            ConfigStore.Config.Ui.AddFolderContents = true;
            _originalUiConfig = _CloneUiConfig(ConfigStore.Config.Ui);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStore.Config.Ui.AddFiles = _originalUiConfig.AddFiles;
            ConfigStore.Config.Ui.AddFolders = _originalUiConfig.AddFolders;
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
        /// Verifies Add All adds every masked file in the current folder.
        /// </summary>
        [Fact]
        public void AddAll_Adds_Masked_Files_In_Current_Folder()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.Mask = "*.txt";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            renameListViewModel.AddAllCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add All is disabled on the This PC drive list.
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

            Assert.False(renameListViewModel.AddAllCommand.CanExecute(null));
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
            ConfigStore.Config.Ui.AddFolders = true;
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
        /// Verifies Add Selected on a folder with contents off stays one level (files and immediate child folders).
        /// </summary>
        [Fact]
        public void AddSelected_Folder_ContentsOff_AddsOneLevel()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddFolders = true;
            ConfigStore.Config.Ui.AddFolderContents = false;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            var names = _PreviewNames(renameListViewModel);
            Assert.Contains("album", names);
            Assert.Contains("disc1", names);
            Assert.Contains("track.mp3", names);
            Assert.Contains("readme.txt", names);
            Assert.DoesNotContain("nested.mp3", names);
        }

        /// <summary>
        /// Verifies Add All adds nested files matching the File List mask.
        /// </summary>
        [Fact]
        public void AddAll_Adds_Nested_Masked_Files()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(albumPath);
            fileListViewModel.Mask = "*.mp3";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            renameListViewModel.AddAllCommand.Execute(null);

            Assert.Equal(
                ["nested.mp3", "track.mp3"],
                _PreviewNames(renameListViewModel).OrderBy(n => n, StringComparer.Ordinal)
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
        /// Verifies Add Selected with files off and folders on adds only the selected folder row.
        /// </summary>
        [Fact]
        public void AddSelected_Folder_FilesOffFoldersOn_AddsFolderOnly()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddFiles = false;
            ConfigStore.Config.Ui.AddFolders = true;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            renameListViewModel.AddSelectedCommand.Execute(null);

            Assert.Equal(["album"], _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies Add Selected is enabled for a folder when files or folders may be added.
        /// </summary>
        [Fact]
        public void AddSelected_CanExecute_FolderWhenFilesOrFoldersOn()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(parent);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));

            ConfigStore.Config.Ui.AddFiles = false;
            ConfigStore.Config.Ui.AddFolders = true;
            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));

            ConfigStore.Config.Ui.AddFolders = false;
            Assert.False(renameListViewModel.AddSelectedCommand.CanExecute(null));
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
                AddFiles = source.AddFiles,
                AddFolders = source.AddFolders,
                AddFolderContents = source.AddFolderContents,
                RememberWindowState = source.RememberWindowState,
                RememberLastFolder = source.RememberLastFolder,
            };
        }
    }
}
