using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
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
            _originalUiConfig = _CloneUiConfig(ConfigStore.Config.Ui);
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            ConfigStore.Config.Ui.AddFolderContents = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStore.Config.Ui.AddMode = _originalUiConfig.AddMode;
            ConfigStore.Config.Ui.AddFolderContents = _originalUiConfig.AddFolderContents;
            ConfigStore.Config.Ui.RememberWindowState = _originalUiConfig.RememberWindowState;
            ConfigStore.Config.Ui.RememberLastFolder = _originalUiConfig.RememberLastFolder;

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
        /// Verifies Remove Selected drops rows, updates CanExecute, and keeps selection at the same index.
        /// </summary>
        [Fact]
        public async Task RemoveSelected_Drops_Rows_And_Keeps_Selection_At_Index()
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
            Assert.Equal(3, renameListViewModel.ItemCount);
            Assert.False(renameListViewModel.RemoveSelectedCommand.CanExecute(null));

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[1]]);
            Assert.True(renameListViewModel.RemoveSelectedCommand.CanExecute(null));
            renameListViewModel.RemoveSelectedCommand.Execute(null);

            Assert.Equal(["alpha.txt", "gamma.log"], _PreviewNames(renameListViewModel));
            Assert.Single(renameListViewModel.SelectedEntries);
            Assert.Equal("gamma.log", renameListViewModel.SelectedEntries[0].FullFileName);
            Assert.Same(renameListViewModel.Entries[1], renameListViewModel.SelectedEntries[0]);
            Assert.Equal(2, renameListViewModel.ItemCount);
            Assert.True(renameListViewModel.RemoveSelectedCommand.CanExecute(null));
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
        /// Verifies SetDropMarkIndex ignores out-of-range indices.
        /// </summary>
        [Fact]
        public async Task SetDropMarkIndex_Out_Of_Range_Clears()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            renameListViewModel.SetDropMarkIndex(0);
            Assert.Equal(0, renameListViewModel.DropMarkIndex);

            renameListViewModel.SetDropMarkIndex(5);
            Assert.Null(renameListViewModel.DropMarkIndex);
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
        /// Verifies Shift+click append adds, toggles, and removes sort keys.
        /// </summary>
        [Fact]
        public void SortByFieldKey_Append_Cycles_Keys()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
            );
            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                append: true
            );

            Assert.Equal(
                [
                    new RenameListSortKey(RenameListTestHelpers.FileFolderKey),
                    new RenameListSortKey(RenameListTestHelpers.ParentFolderKey),
                ],
                renameListViewModel.SortKeys
            );
            Assert.Equal(1, renameListViewModel.ColumnSortStates[RenameListTestHelpers.FileFolderKey].Priority);
            Assert.Equal(2, renameListViewModel.ColumnSortStates[RenameListTestHelpers.ParentFolderKey].Priority);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                append: true
            );
            Assert.Equal(
                [
                    new RenameListSortKey(RenameListTestHelpers.FileFolderKey),
                    new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true),
                ],
                renameListViewModel.SortKeys
            );
            Assert.True(renameListViewModel.ColumnSortStates[RenameListTestHelpers.ParentFolderKey].IsDescending);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                append: true
            );
            Assert.Equal([new RenameListSortKey(RenameListTestHelpers.FileFolderKey)], renameListViewModel.SortKeys);
            Assert.False(renameListViewModel.ColumnSortStates[RenameListTestHelpers.ParentFolderKey].IsActive);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)
            );
            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                append: true
            );
            Assert.Equal(
                [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                renameListViewModel.SortKeys
            );
        }

        /// <summary>
        /// Verifies removing the last sort key via Shift+click turns Auto-Sort off without resorting.
        /// </summary>
        [Fact]
        public async Task SortByFieldKey_Append_Removing_Last_Key_Disables_AutoSort()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "gamma.log"),
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.Equal(["gamma.log", "alpha.txt", "beta.md"], _PreviewNames(renameListViewModel));

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
            );
            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                append: true
            );
            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                append: true
            );

            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Empty(renameListViewModel.SortKeys);
            Assert.Equal(["gamma.log", "beta.md", "alpha.txt"], _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies a plain header click still replaces the entire sort key list.
        /// </summary>
        [Fact]
        public void SortByFieldKey_Replace_Still_Replaces_Entire_List()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)
            );
            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
            );

            Assert.Equal([new RenameListSortKey(RenameListTestHelpers.FullFileNameKey)], renameListViewModel.SortKeys);
            Assert.False(renameListViewModel.ColumnSortStates[RenameListTestHelpers.ParentFolderKey].IsActive);
        }

        /// <summary>
        /// Verifies the sort editor API sets keys and updates summary plus column glyphs.
        /// </summary>
        [Fact]
        public void SetSortKeys_Updates_Summary_And_ColumnStates()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);

            renameListViewModel.SetSortKeys([
                new RenameListSortKey(RenameListTestHelpers.FullPathKey),
                new RenameListSortKey(RenameListTestHelpers.FileFolderKey, Descending: true),
            ]);

            Assert.Equal(
                [
                    new RenameListSortKey(RenameListTestHelpers.FullPathKey),
                    new RenameListSortKey(RenameListTestHelpers.FileFolderKey, Descending: true),
                ],
                renameListViewModel.SortKeys
            );
            Assert.Equal("1. Full File Path ↑\n2. File/Folder ↓", renameListViewModel.SortSummaryText);
            Assert.Equal(2, renameListViewModel.ColumnSortStates[RenameListTestHelpers.FileFolderKey].Priority);
            Assert.True(renameListViewModel.ColumnSortStates[RenameListTestHelpers.FileFolderKey].IsDescending);
            Assert.False(renameListViewModel.ColumnSortStates[RenameListTestHelpers.ParentFolderKey].IsActive);
        }

        /// <summary>
        /// Verifies OpenFieldShuttle opens the unified field shuttle on the Columns tab by default.
        /// </summary>
        [Fact]
        public void OpenFieldShuttle_Raises_FieldShuttleRequested_With_Columns_Tab()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            RenameListFieldShuttleTab? tab = null;
            renameListViewModel.FieldShuttleRequested += (_, requestedTab) => tab = requestedTab;

            renameListViewModel.OpenFieldShuttleCommand.Execute(null);

            Assert.True(renameListViewModel.OpenFieldShuttleCommand.CanExecute(null));
            Assert.Equal(RenameListFieldShuttleTab.Columns, tab);
        }

        /// <summary>
        /// Verifies OpenEditSortFields opens the unified field shuttle on the Sort tab.
        /// </summary>
        [Fact]
        public void OpenEditSortFields_Raises_FieldShuttleRequested_With_Sort_Tab()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            RenameListFieldShuttleTab? tab = null;
            renameListViewModel.FieldShuttleRequested += (_, requestedTab) => tab = requestedTab;

            renameListViewModel.OpenEditSortFieldsCommand.Execute(null);

            Assert.True(renameListViewModel.OpenEditSortFieldsCommand.CanExecute(null));
            Assert.Equal(RenameListFieldShuttleTab.Sort, tab);
        }

        /// <summary>
        /// Verifies preview and unmapped field keys do not change Auto-Sort.
        /// </summary>
        [Fact]
        public void SortByFieldKey_Preview_And_Unmapped_Are_NoOps()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.SetSortKeys([new RenameListSortKey(RenameListTestHelpers.FullFileNameKey)]);

            renameListViewModel.SortByFieldKey(
                RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
            );
            renameListViewModel.SortByFieldKey(RenameListFieldKey.Original("Unknown", "Missing"));

            Assert.Equal([new RenameListSortKey(RenameListTestHelpers.FullFileNameKey)], renameListViewModel.SortKeys);
        }

        /// <summary>
        /// Verifies SetSortKeys drops preview, unknown, and duplicate field keys.
        /// </summary>
        [Fact]
        public void SetSortKeys_Drops_Preview_Unknown_And_Duplicate_Keys()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var unknownKey = RenameListFieldKey.Original("Unknown", "Missing");

            renameListViewModel.SetSortKeys([
                new RenameListSortKey(previewKey),
                new RenameListSortKey(RenameListTestHelpers.FullFileNameKey),
                new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true),
                new RenameListSortKey(unknownKey),
            ]);

            Assert.Equal([new RenameListSortKey(RenameListTestHelpers.FullFileNameKey)], renameListViewModel.SortKeys);
        }

        /// <summary>
        /// Verifies ApplySession drops unsortable keys and keeps the rest.
        /// </summary>
        [Fact]
        public void ApplySession_Drops_Unsortable_Keys()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            renameListViewModel.ApplySession([
                new SessionStateRenameListSortField(previewKey),
                new SessionStateRenameListSortField(RenameListTestHelpers.ParentFolderKey, Descending: true),
            ]);

            Assert.Equal(
                [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                renameListViewModel.SortKeys
            );
        }

        /// <summary>
        /// Verifies Auto-Sort adds append then resort, ignoring selection insert.
        /// </summary>
        [Fact]
        public async Task AutoSort_Add_Appends_And_Resorts_Ignoring_Selection()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.ApplySession(RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey));
            Assert.True(renameListViewModel.IsAutoSort);

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "gamma.log"), _FileEntry(dir, "alpha.txt")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.Equal(["alpha.txt", "gamma.log"], _PreviewNames(renameListViewModel));

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);
            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            Assert.Equal(["alpha.txt", "beta.md", "gamma.log"], _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies manual move cancels Auto-Sort.
        /// </summary>
        [Fact]
        public async Task MoveSelected_Cancels_AutoSort()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.ApplySession(RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey));

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.True(renameListViewModel.IsAutoSort);

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[2]]);
            renameListViewModel.MoveSelectedUpCommand.Execute(null);

            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Empty(renameListViewModel.SortKeys);
            Assert.Equal(["alpha.txt", "gamma.log", "beta.md"], _PreviewNames(renameListViewModel));
        }

        /// <summary>
        /// Verifies ToggleAutoSort restores the default visible-column keys.
        /// </summary>
        [Fact]
        public void ToggleAutoSort_Restores_Default_Keys()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            Assert.False(renameListViewModel.IsAutoSort);

            renameListViewModel.ToggleAutoSortCommand.Execute(null);

            Assert.True(renameListViewModel.IsAutoSort);
            Assert.Equal(RenameListSortKey.DefaultKeys, renameListViewModel.SortKeys);

            renameListViewModel.ToggleAutoSortCommand.Execute(null);
            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Empty(renameListViewModel.SortKeys);
        }

        /// <summary>
        /// Verifies a missing session value restores the default Auto-Sort keys.
        /// </summary>
        [Fact]
        public void ApplySession_Null_Uses_Default()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            Assert.False(renameListViewModel.IsAutoSort);

            renameListViewModel.ApplySession(null);

            Assert.True(renameListViewModel.IsAutoSort);
            Assert.Equal(
                SessionStateRenameList.FromSortKeys(RenameListSortKey.DefaultKeys),
                renameListViewModel.CaptureSortFields()
            );
        }

        /// <summary>
        /// Verifies an empty session value disables Auto-Sort and round-trips through capture.
        /// </summary>
        [Fact]
        public void ApplySession_Empty_Disables_And_Capture_RoundTrips()
        {
            var dir = _CreateSampleFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.ApplySession(
                RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey, descending: true)
            );

            renameListViewModel.ApplySession([]);

            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Empty(renameListViewModel.CaptureSortFields());

            var restored = new RenameListViewModel(_CreateFileListViewModel(dir));
            restored.ApplySession(renameListViewModel.CaptureSortFields());
            Assert.False(restored.IsAutoSort);
        }

        /// <summary>
        /// Verifies SetDropMarkIndex is ignored while Auto-Sort is on (external drop target).
        /// </summary>
        [Fact]
        public async Task AutoSort_Ignores_DropMark()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.ApplySession(RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey));

            fileListViewModel.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);

            renameListViewModel.SetDropMarkIndex(1);
            Assert.Null(renameListViewModel.DropMarkIndex);
        }

        /// <summary>
        /// Verifies CancelAutoSort disables sorting without reordering and allows a drop mark.
        /// </summary>
        [Fact]
        public async Task CancelAutoSort_Disables_Without_Resort_And_Allows_DropMark()
        {
            var dir = _CreateThreeFileFolder();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            renameListViewModel.ApplySession(RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey));

            fileListViewModel.SetSelectedEntries([
                _FileEntry(dir, "alpha.txt"),
                _FileEntry(dir, "beta.md"),
                _FileEntry(dir, "gamma.log"),
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.True(renameListViewModel.IsAutoSort);
            var orderBefore = _PreviewNames(renameListViewModel);

            renameListViewModel.CancelAutoSort();

            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Equal(orderBefore, _PreviewNames(renameListViewModel));

            renameListViewModel.SetDropMarkIndex(1);
            Assert.Equal(1, renameListViewModel.DropMarkIndex);
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
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                path,
                NullFileShellOpener.Instance
            );
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
                var fileListViewModel = new FileListViewModel(
                    NullSystemIconProvider.Instance,
                    parent,
                    NullFileShellOpener.Instance
                );
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
