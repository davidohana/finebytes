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
        private readonly List<FileListViewModel> _fileLists = [];
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

            foreach (var fileList in _fileLists)
            {
                fileList.Dispose();
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
            var fileList = _CreateFileList(dir);
            var renameList = new RenameListViewModel(fileList);

            var alpha = _FileEntry(dir, "alpha.txt");
            var beta = _FileEntry(dir, "beta.md");
            fileList.SetSelectedEntries([alpha, beta]);

            renameList.AddSelectedCommand.Execute(null);

            Assert.Equal(2, renameList.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(renameList));

            fileList.SetSelectedEntries([alpha]);
            renameList.AddSelectedCommand.Execute(null);

            Assert.Equal(2, renameList.Entries.Count);
        }

        /// <summary>
        /// Verifies Add Selected honors the File List include mask.
        /// </summary>
        [Fact]
        public void AddSelected_Honors_Include_Mask()
        {
            var dir = _CreateSampleFolder();
            var fileList = _CreateFileList(dir);
            fileList.Mask = "*.txt";
            var renameList = new RenameListViewModel(fileList);

            fileList.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            renameList.AddSelectedCommand.Execute(null);

            Assert.Single(renameList.Entries);
            Assert.Equal("alpha.txt", renameList.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add Selected honors enabled exclude masks.
        /// </summary>
        [Fact]
        public void AddSelected_Honors_Exclude_Masks()
        {
            var dir = _CreateSampleFolder();
            var fileList = _CreateFileList(dir);
            fileList.ApplyExcludeMasks(enabled: true, editorText: "*.txt");
            var renameList = new RenameListViewModel(fileList);

            fileList.SetSelectedEntries([_FileEntry(dir, "alpha.txt"), _FileEntry(dir, "beta.md")]);
            renameList.AddSelectedCommand.Execute(null);

            Assert.Single(renameList.Entries);
            Assert.Equal("beta.md", renameList.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies Add All adds every masked file in the current folder.
        /// </summary>
        [Fact]
        public void AddAll_Adds_Masked_Files_In_Current_Folder()
        {
            var dir = _CreateSampleFolder();
            var fileList = _CreateFileList(dir);
            fileList.Mask = "*.txt";
            var renameList = new RenameListViewModel(fileList);

            renameList.AddAllCommand.Execute(null);

            Assert.Single(renameList.Entries);
            Assert.Equal("alpha.txt", renameList.Entries[0].FullFileName);
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

            var fileList = _CreateFileList(_tempDirectoryFixture.CreateTempDir());
            fileList.NavigateTo(FileListViewModel.ComputerPath);
            var renameList = new RenameListViewModel(fileList);

            Assert.False(renameList.AddAllCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies preview names match originals before filter preview exists.
        /// </summary>
        [Fact]
        public void Entries_Show_Identity_Preview()
        {
            var dir = _CreateSampleFolder();
            var fileList = _CreateFileList(dir);
            var renameList = new RenameListViewModel(fileList);

            fileList.SetSelectedEntries([_FileEntry(dir, "alpha.txt")]);
            renameList.AddSelectedCommand.Execute(null);

            Assert.Equal("alpha.txt", renameList.Entries[0].FullFileName);
            Assert.Equal("alpha.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal("File", renameList.Entries[0].FileFolder);
        }

        private string _CreateSampleFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private FileListViewModel _CreateFileList(string path)
        {
            var fileList = new FileListViewModel(NullSystemIconProvider.Instance, path);
            _fileLists.Add(fileList);
            return fileList;
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

        private static IReadOnlyList<string> _PreviewNames(RenameListViewModel renameList)
        {
            return [.. renameList.Entries.Select(entry => entry.FullFileName)];
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
