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
        private readonly string _dir;

        /// <summary>
        /// Creates an isolated temp tree for adapter tests.
        /// </summary>
        public RenameListAddSourcesTests()
        {
            _dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(_dir, "album"));
            File.WriteAllText(Path.Combine(_dir, "alpha.txt"), "a");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies selection of a folder becomes that folder plus the File List mask.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_Folder_ReturnsFolderPlusMask()
        {
            var albumPath = Path.Combine(_dir, "album");
            var selectedEntries = new[]
            {
                new FileListEntry
                {
                    Name = "album",
                    FullPath = albumPath,
                    IsDirectory = true,
                },
            };

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*.mp3",
                addMode: RenameListAddMode.Files
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
            var sources = RenameListAddSources.ResolveSourcesFromCurrentFolder(
                currentPath: _dir,
                mask: "*.txt"
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(_dir, "*.txt"), source);
            Assert.DoesNotContain("**", source, StringComparison.Ordinal);
            Assert.True(RenameListAddSources.CanResolveFromCurrentFolder(_dir));
        }

        /// <summary>
        /// Verifies a selected file that matches the mask is passed as a raw path.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_MatchingFile_ReturnsRawPath()
        {
            var filePath = Path.Combine(_dir, "alpha.txt");
            var selectedEntries = new[]
            {
                new FileListEntry
                {
                    Name = "alpha.txt",
                    FullPath = filePath,
                    IsDirectory = false,
                },
            };

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*.txt",
                addMode: RenameListAddMode.Files
            );

            var source = Assert.Single(sources);
            Assert.Equal(filePath, source);
        }

        /// <summary>
        /// Verifies folders-only mode skips selected files but still emits folder sources.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_FoldersOnly_SkipsFiles()
        {
            var albumPath = Path.Combine(_dir, "album");
            var filePath = Path.Combine(_dir, "alpha.txt");
            var selectedEntries = new[]
            {
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
            };

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*",
                addMode: RenameListAddMode.Folders
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(albumPath, "*"), source);
            Assert.True(
                RenameListAddSources.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.Folders
                )
            );
            Assert.False(
                RenameListAddSources.CanResolveFromSelection(
                    [selectedEntries[1]],
                    mask: "*",
                    addMode: RenameListAddMode.Folders
                )
            );
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

            var root = Path.GetPathRoot(_dir);
            Assert.False(string.IsNullOrEmpty(root));
            var selectedEntries = new[]
            {
                new FileListEntry
                {
                    Name = root,
                    FullPath = root,
                    IsDirectory = true,
                },
            };

            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
            Assert.False(
                RenameListAddSources.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
        }

        /// <summary>
        /// Verifies Add All returns no source for a drive root.
        /// </summary>
        [Fact]
        public void ResolveFromCurrentFolder_RootPath_ReturnsEmpty()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var root = Path.GetPathRoot(_dir);
            Assert.False(string.IsNullOrEmpty(root));
            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromCurrentFolder(currentPath: root, mask: "*")
            );
            Assert.False(RenameListAddSources.CanResolveFromCurrentFolder(root));
        }

        /// <summary>
        /// Verifies Add All returns no source for File List sentinel locations.
        /// </summary>
        [Fact]
        public void ResolveFromCurrentFolder_SentinelPath_ReturnsEmpty()
        {
            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromCurrentFolder(
                    currentPath: FileListPath.ComputerPath,
                    mask: "*"
                )
            );
            Assert.False(RenameListAddSources.CanResolveFromCurrentFolder(FileListPath.ComputerPath));

            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromCurrentFolder(
                    currentPath: FileListPath.NetworkPath,
                    mask: "*"
                )
            );
            Assert.False(RenameListAddSources.CanResolveFromCurrentFolder(FileListPath.NetworkPath));
        }
    }
}
