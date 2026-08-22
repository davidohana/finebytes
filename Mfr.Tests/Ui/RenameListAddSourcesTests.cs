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

        /// <summary>
        /// Verifies both add-policy flags off yields no selection sources.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_BothPolicyOff_ReturnsEmpty()
        {
            var albumPath = Path.Combine(_fileList.CurrentPath, "album");
            var filePath = Path.Combine(_fileList.CurrentPath, "alpha.txt");
            _fileList.SetSelectedEntries([
                new FileListEntry
                {
                    Name = "album",
                    FullPath = albumPath,
                    IsDirectory = true,
                },
                new FileListEntry
                {
                    Name = "alpha.txt",
                    FullPath = filePath,
                    IsDirectory = false,
                },
            ]);

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                _fileList,
                addFiles: false,
                addFolders: false
            );

            Assert.Empty(sources);
            Assert.False(RenameListAddSources.CanResolveFromSelection(_fileList, addFiles: false, addFolders: false));
        }

        /// <summary>
        /// Verifies drive-root selection is not addable.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_RootPath_ReturnsEmpty()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var root = Path.GetPathRoot(_fileList.CurrentPath);
            Assert.False(string.IsNullOrEmpty(root));
            _fileList.SetSelectedEntries([
                new FileListEntry
                {
                    Name = root,
                    FullPath = root,
                    IsDirectory = true,
                },
            ]);

            Assert.Empty(RenameListAddSources.ResolveSourcesFromSelection(_fileList, addFiles: true, addFolders: true));
            Assert.False(RenameListAddSources.CanResolveFromSelection(_fileList, addFiles: true, addFolders: true));
        }

        /// <summary>
        /// Verifies Add All CanResolve matches Resolve emptiness when policy is off.
        /// </summary>
        [Fact]
        public void CanResolveFromCurrentFolder_BothPolicyOff_IsFalse()
        {
            Assert.False(
                RenameListAddSources.CanResolveFromCurrentFolder(_fileList, addFiles: false, addFolders: false)
            );
            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromCurrentFolder(_fileList, addFiles: false, addFolders: false)
            );
        }
    }
}
