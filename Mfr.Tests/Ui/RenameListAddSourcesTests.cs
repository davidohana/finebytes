using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests that the Rename List adapter emits folder+mask sources rather than expanding globs.
    /// </summary>
    public sealed class RenameListAddSourcesTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly FileListViewModel _fileList;

        /// <summary>
        /// Creates an isolated File List for adapter tests.
        /// </summary>
        public RenameListAddSourcesTests()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "album"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            _fileList = new FileListViewModel(NullSystemIconProvider.Instance, dir);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _fileList.Dispose();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies selection of a folder becomes that folder plus the File List mask.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_Folder_ReturnsFolderPlusMask()
        {
            var albumPath = Path.Combine(_fileList.CurrentPath, "album");
            _fileList.Mask = "*.mp3";
            _fileList.SetSelectedEntries([
                new FileListEntry
                {
                    Name = "album",
                    FullPath = albumPath,
                    IsDirectory = true,
                },
            ]);

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                _fileList,
                addFiles: true,
                addFolders: false
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(albumPath, "*.mp3"), source);
            Assert.DoesNotContain("**", source, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies Add All returns the current folder plus the File List mask.
        /// </summary>
        [Fact]
        public void ResolveFromCurrentFolder_ReturnsCurrentFolderPlusMask()
        {
            _fileList.Mask = "*.txt";
            var sources = RenameListAddSources.ResolveSourcesFromCurrentFolder(
                _fileList,
                addFiles: true,
                addFolders: false
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(_fileList.CurrentPath, "*.txt"), source);
            Assert.DoesNotContain("**", source, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies a selected file that matches the mask is passed as a raw path.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_MatchingFile_ReturnsRawPath()
        {
            var filePath = Path.Combine(_fileList.CurrentPath, "alpha.txt");
            _fileList.Mask = "*.txt";
            _fileList.SetSelectedEntries([
                new FileListEntry
                {
                    Name = "alpha.txt",
                    FullPath = filePath,
                    IsDirectory = false,
                },
            ]);

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                _fileList,
                addFiles: true,
                addFolders: false
            );

            var source = Assert.Single(sources);
            Assert.Equal(filePath, source);
        }
    }
}
