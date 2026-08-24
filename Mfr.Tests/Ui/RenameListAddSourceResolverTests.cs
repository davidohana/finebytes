using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests that the Rename List adapter emits folder+mask sources rather than expanding globs.
    /// </summary>
    public sealed class RenameListAddSourceResolverTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly string _dir;

        /// <summary>
        /// Creates an isolated temp tree for adapter tests.
        /// </summary>
        public RenameListAddSourceResolverTests()
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
        /// Verifies a selected folder becomes that folder plus the File List mask.
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

            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*.mp3",
                addMode: RenameListAddMode.Files
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(albumPath, "*.mp3"), source);
            Assert.DoesNotContain("**", source, StringComparison.Ordinal);
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

            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
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

            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*",
                addMode: RenameListAddMode.Folders
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(albumPath, "*"), source);
            Assert.True(
                RenameListAddSourceResolver.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.Folders
                )
            );
            Assert.False(
                RenameListAddSourceResolver.CanResolveFromSelection(
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
                RenameListAddSourceResolver.ResolveSourcesFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
            Assert.False(
                RenameListAddSourceResolver.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
        }

        /// <summary>
        /// Verifies Add All browse-location gate rejects only This PC / Network, not drive roots.
        /// </summary>
        [Fact]
        public void CanAddAllFrom_Rejects_Sentinels_Allows_Drive_Roots()
        {
            Assert.False(RenameListAddSourceResolver.CanAddAllFrom(FileListPath.ComputerPath));
            Assert.True(RenameListAddSourceResolver.CanAddAllFrom(_dir));

            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.False(RenameListAddSourceResolver.CanAddAllFrom(FileListPath.NetworkPath));
            var root = Path.GetPathRoot(_dir);
            Assert.False(string.IsNullOrEmpty(root));
            Assert.True(RenameListAddSourceResolver.CanAddAllFrom(root));
        }

        /// <summary>
        /// Verifies File List sentinel locations are not addable as selection sources.
        /// </summary>
        [Fact]
        public void ResolveFromSelection_SentinelPath_ReturnsEmpty()
        {
            var selectedEntries = new[]
            {
                new FileListEntry
                {
                    Name = FileListPath.ComputerDisplayName,
                    FullPath = FileListPath.ComputerPath,
                    IsDirectory = true,
                },
            };

            Assert.Empty(
                RenameListAddSourceResolver.ResolveSourcesFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
            Assert.False(
                RenameListAddSourceResolver.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );

            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            selectedEntries =
            [
                new FileListEntry
                {
                    Name = FileListPath.NetworkDisplayName,
                    FullPath = FileListPath.NetworkPath,
                    IsDirectory = true,
                },
            ];

            Assert.Empty(
                RenameListAddSourceResolver.ResolveSourcesFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
            Assert.False(
                RenameListAddSourceResolver.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addMode: RenameListAddMode.FilesAndFolders
                )
            );
        }

        /// <summary>
        /// Verifies dropped paths use Directory.Exists for folder+mask sources.
        /// </summary>
        [Fact]
        public void ResolveFromPaths_FolderAndFile_MatchSelectionRules()
        {
            var albumPath = Path.Combine(_dir, "album");
            var filePath = Path.Combine(_dir, "alpha.txt");

            var sources = RenameListAddSourceResolver.ResolveSourcesFromPaths(
                [albumPath, filePath],
                mask: "*.mp3",
                addMode: RenameListAddMode.Files
            );

            Assert.Equal(2, sources.Count);
            Assert.Contains(Path.Combine(albumPath, "*.mp3"), sources);
            Assert.Contains(filePath, sources);
        }

        /// <summary>
        /// Verifies folders-only mode skips file paths from drag-drop.
        /// </summary>
        [Fact]
        public void ResolveFromPaths_FoldersOnly_SkipsFiles()
        {
            var albumPath = Path.Combine(_dir, "album");
            var filePath = Path.Combine(_dir, "alpha.txt");

            var sources = RenameListAddSourceResolver.ResolveSourcesFromPaths(
                [albumPath, filePath],
                mask: "*",
                addMode: RenameListAddMode.Folders
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(albumPath, "*"), source);
        }

        /// <summary>
        /// Verifies IsValidSourcePath rejects roots and sentinels.
        /// </summary>
        [Fact]
        public void IsValidSourcePath_Rejects_Roots_And_Sentinels()
        {
            Assert.True(RenameListAddSourceResolver.IsValidSourcePath(Path.Combine(_dir, "alpha.txt")));
            Assert.False(RenameListAddSourceResolver.IsValidSourcePath(FileListPath.ComputerPath));
            Assert.False(RenameListAddSourceResolver.IsValidSourcePath(""));

            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var root = Path.GetPathRoot(_dir);
            Assert.False(string.IsNullOrEmpty(root));
            Assert.False(RenameListAddSourceResolver.IsValidSourcePath(root));
            Assert.False(RenameListAddSourceResolver.IsValidSourcePath(FileListPath.NetworkPath));
        }
    }
}
