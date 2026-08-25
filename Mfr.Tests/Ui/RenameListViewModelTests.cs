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
        public async Task AddSelected_Adds_Files_And_Ignores_Duplicates()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            var alpha = _FileEntry(dir, "alpha.txt");
            var beta = _FileEntry(dir, "beta.md");
            fileListViewModel.SetSelectedEntries([alpha, beta]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));

            fileListViewModel.SetSelectedEntries([alpha]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
        }

        /// <summary>
        /// Verifies AddPathsAsync adds dropped files with the same rules as Add Selected.
        /// </summary>
        [Fact]
        public async Task AddPaths_Adds_Files_And_Ignores_Duplicates()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));

            await renameListViewModel.AddPathsAsync([alphaPath]);

            Assert.Equal(2, renameListViewModel.Entries.Count);
        }

        /// <summary>
        /// Verifies AddPathsAsync expands a dropped folder using the File List mask.
        /// </summary>
        [Fact]
        public async Task AddPaths_Folder_Uses_FileList_Mask()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(parent);
            fileListViewModel.Mask = "*.mp3";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            await renameListViewModel.AddPathsAsync([albumPath]);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Contains(renameListViewModel.Entries, entry => entry.FullFileName == "track.mp3");
            Assert.Contains(renameListViewModel.Entries, entry => entry.FullFileName == "nested.mp3");
            Assert.DoesNotContain(renameListViewModel.Entries, entry => entry.FullFileName == "readme.txt");
        }

        /// <summary>
        /// Verifies AddPathsAsync honors folders-only AddMode by skipping files.
        /// </summary>
        [Fact]
        public async Task AddPaths_FoldersOnly_Skips_Files()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Folders;
            ConfigStore.Config.Ui.AddFolderContents = false;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            await renameListViewModel.AddPathsAsync([albumPath, Path.Combine(parent, "other.txt")]);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("album", renameListViewModel.Entries[0].FullFileName);
            Assert.Equal("Folder", renameListViewModel.Entries[0].FileFolder);
        }

        /// <summary>
        /// Verifies AddPathsAsync no-ops for empty or non-addable path lists.
        /// </summary>
        [Fact]
        public async Task AddPaths_Empty_Or_NonAddable_Does_Nothing()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            await renameListViewModel.AddPathsAsync([]);
            Assert.Empty(renameListViewModel.Entries);

            await renameListViewModel.AddPathsAsync([FileListPath.ComputerPath]);
            Assert.Empty(renameListViewModel.Entries);
        }

        /// <summary>
        /// Verifies AddPathsAsync is ignored while an add is already running.
        /// </summary>
        [Fact]
        public async Task AddPaths_Blocked_While_IsAdding()
        {
            var parent = _tempDirectoryFixture.CreateTempDir();
            var tree = Path.Combine(parent, "tree");
            Directory.CreateDirectory(tree);
            for (var i = 0; i < 200; i++)
            {
                var nested = Path.Combine(tree, $"d{i:D3}");
                Directory.CreateDirectory(nested);
                File.WriteAllText(Path.Combine(nested, $"f{i:D3}.txt"), "x");
            }

            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(tree)]);

            var addSelected = renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            await _WaitUntil(() => renameListViewModel.IsAdding);

            await renameListViewModel.AddPathsAsync([Path.Combine(tree, "d000", "f000.txt")]);
            await addSelected;

            Assert.Equal(200, renameListViewModel.Entries.Count);
        }

        /// <summary>
        /// Verifies the include mask hides non-matching files from Add Selected.
        /// </summary>
        [Fact]
        public async Task AddSelected_Honors_Include_Mask()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.Mask = "*.txt";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.DoesNotContain(fileListViewModel.Entries, entry => entry.Name == "beta.md");

            var alpha = Assert.Single(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            fileListViewModel.SetSelectedEntries([alpha]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies enabled exclude masks hide matching files from Add Selected.
        /// </summary>
        [Fact]
        public async Task AddSelected_Honors_Exclude_Masks()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.ApplyExcludeMasks(enabled: true, editorText: "*.txt");
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.DoesNotContain(fileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "beta.md");

            var beta = Assert.Single(fileListViewModel.Entries, entry => entry.Name == "beta.md");
            fileListViewModel.SetSelectedEntries([beta]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("beta.md", renameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add All adds every listed File List row without needing a selection.
        /// </summary>
        [Fact]
        public async Task AddAll_Adds_Listed_Masked_Files_Without_Selection()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            fileListViewModel.Mask = "*.txt";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Empty(fileListViewModel.SelectedEntries);
            await renameListViewModel.AddAllCommand.ExecuteAsync(null);

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
            Assert.False(RenameListAddSourceResolver.CanAddAllFrom(fileListViewModel.CurrentPath));
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

            Assert.True(RenameListAddSourceResolver.CanAddAllFrom(fileListViewModel.CurrentPath));
            Assert.True(renameListViewModel.AddAllCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies preview names match originals before filter preview exists.
        /// </summary>
        [Fact]
        public async Task Entries_Show_Identity_Preview()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileName);
            Assert.Equal("alpha.txt", renameListViewModel.Entries[0].FullFileNamePreview);
            Assert.Equal("File", renameListViewModel.Entries[0].FileFolder);
        }

        /// <summary>
        /// Verifies Add Selected on a folder adds matching files recursively and no folder row (UI defaults).
        /// </summary>
        [Fact]
        public async Task AddSelected_Folder_Default_AddsNestedFilesNotFolder()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

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
        public async Task AddSelected_Folder_AddFoldersAndContents_AddsNestedFolderRows()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.FilesAndFolders;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

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
        public async Task AddSelected_Folder_ContentsOff_AddsFolderAndTopLevelFiles()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.FilesAndFolders;
            ConfigStore.Config.Ui.AddFolderContents = false;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

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
        public async Task AddAll_Expands_Listed_Folders_Like_AddSelected()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var fileListViewModel = _CreateFileListViewModel(albumPath);
            fileListViewModel.Mask = "*.mp3";
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "disc1" && entry.IsDirectory);
            Assert.Contains(fileListViewModel.Entries, entry => entry.Name == "track.mp3");

            await renameListViewModel.AddAllCommand.ExecuteAsync(null);

            Assert.Equal(
                ["nested.mp3", "track.mp3"],
                _PreviewNames(renameListViewModel).OrderBy(n => n, StringComparer.Ordinal)
            );
        }

        /// <summary>
        /// Verifies Add All matches Add Selected when every listed row is selected.
        /// </summary>
        [Fact]
        public async Task AddAll_Matches_AddSelected_When_All_Listed_Rows_Selected()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var other = Path.Combine(parent, "other.txt");
            File.WriteAllText(other, "o");

            var addAllList = _CreateFileListViewModel(parent);
            var addAllRename = new RenameListViewModel(addAllList);
            await addAllRename.AddAllCommand.ExecuteAsync(null);

            var addSelectedList = _CreateFileListViewModel(parent);
            var addSelectedRename = new RenameListViewModel(addSelectedList);
            addSelectedList.SetSelectedEntries([.. addSelectedList.Entries]);
            await addSelectedRename.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(
                _PreviewNames(addAllRename).OrderBy(n => n, StringComparer.Ordinal),
                _PreviewNames(addSelectedRename).OrderBy(n => n, StringComparer.Ordinal)
            );
        }

        /// <summary>
        /// Verifies Add Selected can mix an exact file with a folder source.
        /// </summary>
        [Fact]
        public async Task AddSelected_MixedFileAndFolder_AddsBoth()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var other = Path.Combine(parent, "other.txt");
            File.WriteAllText(other, "o");
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FileEntry(parent, "other.txt"), _FolderEntry(albumPath)]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            var names = _PreviewNames(renameListViewModel);
            Assert.Contains("other.txt", names);
            Assert.Contains("track.mp3", names);
        }

        /// <summary>
        /// Verifies Add Selected with files off and folders on adds the selected folder and descendant folders.
        /// </summary>
        [Fact]
        public async Task AddSelected_Folder_FilesOffFoldersOn_AddsFolderAndDescendants()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Folders;
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);

            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

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
        public async Task RemoveSelected_Removes_Only_Selected_Rows()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.Equal(2, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.RemoveSelectedCommand.CanExecute(null));

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);
            Assert.True(renameListViewModel.RemoveSelectedCommand.CanExecute(null));

            renameListViewModel.RemoveSelectedCommand.Execute(null);

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("beta.md", renameListViewModel.Entries[0].FullFileName);
            Assert.Single(renameListViewModel.SelectedEntries);
            Assert.Same(renameListViewModel.Entries[0], renameListViewModel.SelectedEntries[0]);
            Assert.Equal(1, renameListViewModel.ItemCount);
            Assert.True(renameListViewModel.RemoveSelectedCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Remove Selected keeps focus on the row that slides into the deleted index.
        /// </summary>
        [Fact]
        public async Task RemoveSelected_Selects_Row_At_Same_Index()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[1]]);
            renameListViewModel.RemoveSelectedCommand.Execute(null);

            Assert.Equal(["alpha.txt", "gamma.log"], _PreviewNames(renameListViewModel));
            Assert.Single(renameListViewModel.SelectedEntries);
            Assert.Equal("gamma.log", renameListViewModel.SelectedEntries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add appends new rows without recreating existing entry objects.
        /// </summary>
        [Fact]
        public async Task AddSelected_Preserves_Existing_Entry_Identity()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            var alphaEntry = renameListViewModel.Entries[0];
            var betaEntry = renameListViewModel.Entries[1];

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "gamma.log")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(3, renameListViewModel.Entries.Count);
            Assert.Same(alphaEntry, renameListViewModel.Entries[0]);
            Assert.Same(betaEntry, renameListViewModel.Entries[1]);
            Assert.Equal("gamma.log", renameListViewModel.Entries[2].FullFileName);
        }

        /// <summary>
        /// Verifies Add with a Rename List selection inserts after the first selected row (MFR7 help).
        /// </summary>
        [Fact]
        public async Task AddSelected_With_RenameList_Selection_Inserts_After_And_Selects_First_New()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "gamma.log")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            var alphaEntry = renameListViewModel.Entries[0];
            var gammaEntry = renameListViewModel.Entries[1];

            renameListViewModel.SetSelectedEntries([gammaEntry]);
            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(["alpha.txt", "gamma.log", "beta.md"], _PreviewNames(renameListViewModel));
            Assert.Same(alphaEntry, renameListViewModel.Entries[0]);
            Assert.Same(gammaEntry, renameListViewModel.Entries[1]);
            Assert.Single(renameListViewModel.SelectedEntries);
            Assert.Equal("beta.md", renameListViewModel.SelectedEntries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add with no Rename List selection still appends.
        /// </summary>
        [Fact]
        public async Task AddSelected_Without_RenameList_Selection_Appends()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "gamma.log")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            renameListViewModel.SetSelectedEntries([]);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(["alpha.txt", "gamma.log", "beta.md"], _PreviewNames(renameListViewModel));
            Assert.Empty(renameListViewModel.SelectedEntries);
        }

        /// <summary>
        /// Verifies Remove keeps remaining row objects and order.
        /// </summary>
        [Fact]
        public async Task RemoveSelected_Preserves_Remaining_Entry_Identity()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            var alphaEntry = renameListViewModel.Entries[0];
            var gammaEntry = renameListViewModel.Entries[2];

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[1]]);
            renameListViewModel.RemoveSelectedCommand.Execute(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Same(alphaEntry, renameListViewModel.Entries[0]);
            Assert.Same(gammaEntry, renameListViewModel.Entries[1]);
            Assert.Equal(["alpha.txt", "gamma.log"], _PreviewNames(renameListViewModel));
            Assert.Single(renameListViewModel.SelectedEntries);
            Assert.Same(gammaEntry, renameListViewModel.SelectedEntries[0]);
        }

        /// <summary>
        /// Verifies Remove All But Selected keeps selected rows and drops the rest.
        /// </summary>
        [Fact]
        public async Task RemoveAllButSelected_Keeps_Only_Selected_Rows()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.False(renameListViewModel.RemoveAllButSelectedCommand.CanExecute(null));

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0], renameListViewModel.Entries[2]]);
            Assert.True(renameListViewModel.RemoveAllButSelectedCommand.CanExecute(null));

            renameListViewModel.RemoveAllButSelectedCommand.Execute(null);

            Assert.Equal(["alpha.txt", "gamma.log"], _PreviewNames(renameListViewModel));
            Assert.Equal(2, renameListViewModel.ItemCount);
            Assert.Equal(2, renameListViewModel.SelectedEntries.Count);
            Assert.Equal("alpha.txt", renameListViewModel.SelectedEntries[0].FullFileName);
            Assert.Equal("gamma.log", renameListViewModel.SelectedEntries[1].FullFileName);
            Assert.False(renameListViewModel.RemoveAllButSelectedCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Remove All But Selected keeps the same entry objects and list order.
        /// </summary>
        [Fact]
        public async Task RemoveAllButSelected_Preserves_Kept_Entry_Identity()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            var alphaEntry = renameListViewModel.Entries[0];
            var gammaEntry = renameListViewModel.Entries[2];

            renameListViewModel.SetSelectedEntries([alphaEntry, gammaEntry]);
            renameListViewModel.RemoveAllButSelectedCommand.Execute(null);

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Same(alphaEntry, renameListViewModel.Entries[0]);
            Assert.Same(gammaEntry, renameListViewModel.Entries[1]);
            Assert.Same(alphaEntry, renameListViewModel.SelectedEntries[0]);
            Assert.Same(gammaEntry, renameListViewModel.SelectedEntries[1]);
        }

        /// <summary>
        /// Verifies Clear empties the list and updates ItemCount / CanExecute.
        /// </summary>
        [Fact]
        public async Task Clear_Removes_All_Rows()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            Assert.False(renameListViewModel.ClearCommand.CanExecute(null));

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.True(renameListViewModel.ClearCommand.CanExecute(null));
            Assert.Equal(2, renameListViewModel.ItemCount);

            renameListViewModel.ClearCommand.Execute(null);

            Assert.Empty(renameListViewModel.Entries);
            Assert.Equal(0, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.ClearCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Move Selected Up reorders a contiguous selection as a block.
        /// </summary>
        [Fact]
        public async Task MoveSelectedUp_Moves_Contiguous_Selection_As_Block()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.False(renameListViewModel.MoveSelectedUpCommand.CanExecute(null));

            var beta = renameListViewModel.Entries[1];
            var gamma = renameListViewModel.Entries[2];
            renameListViewModel.SetSelectedEntries([beta, gamma]);
            Assert.True(renameListViewModel.MoveSelectedUpCommand.CanExecute(null));

            renameListViewModel.MoveSelectedUpCommand.Execute(null);

            Assert.Equal(["beta.md", "gamma.log", "alpha.txt"], _PreviewNames(renameListViewModel));
            Assert.Equal([beta, gamma], renameListViewModel.SelectedEntries);
        }

        /// <summary>
        /// Verifies Move Selected Down reorders a contiguous selection as a block.
        /// </summary>
        [Fact]
        public async Task MoveSelectedDown_Moves_Contiguous_Selection_As_Block()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            var alpha = renameListViewModel.Entries[0];
            var beta = renameListViewModel.Entries[1];
            renameListViewModel.SetSelectedEntries([alpha, beta]);

            renameListViewModel.MoveSelectedDownCommand.Execute(null);

            Assert.Equal(["gamma.log", "alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));
            Assert.Equal([alpha, beta], renameListViewModel.SelectedEntries);
        }

        /// <summary>
        /// Verifies Move Selected Up at the top leaves order unchanged.
        /// </summary>
        [Fact]
        public async Task MoveSelectedUp_At_Top_Is_NoOp()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);
            renameListViewModel.MoveSelectedUpCommand.Execute(null);

            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies Locate in File List navigates to the row folder and selects the item.
        /// </summary>
        [Fact]
        public async Task LocateInFileList_Selects_Row_In_File_List()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            var nestedDir = Path.Combine(albumPath, "disc1");
            var nestedPath = Path.Combine(nestedDir, "nested.mp3");
            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.NavigateTo(parent);
            fileListViewModel.SetSelectedEntries([_FolderEntry(albumPath)]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            var nestedEntry = renameListViewModel.Entries.Single(entry => entry.FullFileName == "nested.mp3");
            renameListViewModel.SetSelectedEntries([nestedEntry]);
            Assert.True(renameListViewModel.LocateInFileListCommand.CanExecute(null));

            renameListViewModel.LocateInFileListCommand.Execute(null);

            Assert.Equal(nestedDir, fileListViewModel.CurrentPath);
            Assert.NotNull(fileListViewModel.SelectedEntry);
            Assert.Equal(nestedPath, fileListViewModel.SelectedEntry.FullPath);
            Assert.Empty(renameListViewModel.LastLocateError);
        }

        /// <summary>
        /// Verifies Locate in File List sets an error when the path is not in the listing.
        /// </summary>
        [Fact]
        public async Task LocateInFileList_Sets_Error_When_Item_Not_Listed()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            fileListViewModel.Mask = "*.md";
            renameListViewModel.LocateInFileListCommand.Execute(null);

            Assert.Contains("Failed to locate", renameListViewModel.LastLocateError);
        }

        /// <summary>
        /// Verifies canceling a long add discards the in-progress batch and leaves add commands enabled.
        /// </summary>
        [Fact]
        public async Task AddSelected_Cancel_Discards_Partial_Batch()
        {
            var parent = _tempDirectoryFixture.CreateTempDir();
            var tree = Path.Combine(parent, "tree");
            Directory.CreateDirectory(tree);
            for (var i = 0; i < 200; i++)
            {
                var nested = Path.Combine(tree, $"d{i:D3}");
                Directory.CreateDirectory(nested);
                File.WriteAllText(Path.Combine(nested, $"f{i:D3}.txt"), "x");
            }

            var fileListViewModel = _CreateFileListViewModel(parent);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            fileListViewModel.SetSelectedEntries([_FolderEntry(tree)]);

            var addTask = renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            await _WaitUntil(() => renameListViewModel.IsAdding).ConfigureAwait(true);

            Assert.False(renameListViewModel.AddSelectedCommand.CanExecute(null));
            renameListViewModel.AddProgress.CancelCommand.Execute(null);
            await addTask.ConfigureAwait(true);

            Assert.False(renameListViewModel.IsAdding);
            Assert.Empty(renameListViewModel.Entries);
            Assert.Equal(0, renameListViewModel.ItemCount);
            Assert.True(renameListViewModel.AddSelectedCommand.CanExecute(null));
        }

        private string _CreateSampleFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private string _CreateThreeFileFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            File.WriteAllText(Path.Combine(dir, "gamma.log"), "g");
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

        private static async Task _WaitUntil(Func<bool> condition)
        {
            for (var i = 0; i < 200; i++)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(10).ConfigureAwait(true);
            }

            Assert.Fail("Timed out waiting for condition.");
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

        /// <summary>
        /// Verifies adding an inaccessible folder sets a skip summary on the Rename List.
        /// </summary>
        [Fact]
        public async Task AddSelected_Inaccessible_Folder_Sets_LastAddError()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var parent = Directory
                .CreateDirectory(
                    Path.Combine(Directory.GetCurrentDirectory(), "mfr_rename_ui_" + Guid.NewGuid().ToString("N"))
                )
                .FullName;
            var deniedFolder = Directory.CreateDirectory(Path.Combine(parent, "Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);

            try
            {
                var fileListViewModel = new FileListViewModel(NullSystemIconProvider.Instance, parent);
                var renameListViewModel = new RenameListViewModel(fileListViewModel);
                var deniedEntry = fileListViewModel.Entries.Single(entry => entry.IsDirectory);
                fileListViewModel.SetSelectedEntries([deniedEntry]);

                await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

                Assert.Empty(renameListViewModel.Entries);
                Assert.Equal("Added 0 item(s). Skipped 1 inaccessible source(s).", renameListViewModel.LastAddError);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
                try
                {
                    Directory.Delete(parent, recursive: true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _DenyDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.AddAccessRule(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _AllowDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.RemoveAccessRuleAll(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }
    }
}
