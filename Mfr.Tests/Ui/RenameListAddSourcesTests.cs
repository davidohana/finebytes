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
            var sources = RenameListAddSources.ResolveSourcesFromCurrentFolder(
                currentPath: _dir,
                mask: "*.txt",
                canAddAllToCurrentFolder: true,
                addFiles: true,
                addFolders: false
            );

            var source = Assert.Single(sources);
            Assert.Equal(Path.Combine(_dir, "*.txt"), source);
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

            var sources = RenameListAddSources.ResolveSourcesFromSelection(
                selectedEntries,
                mask: "*.txt",
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
                addFiles: false,
                addFolders: false
            );

            Assert.Empty(sources);
            Assert.False(
                RenameListAddSources.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addFiles: false,
                    addFolders: false
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
                    addFiles: true,
                    addFolders: true
                )
            );
            Assert.False(
                RenameListAddSources.CanResolveFromSelection(
                    selectedEntries,
                    mask: "*",
                    addFiles: true,
                    addFolders: true
                )
            );
        }

        /// <summary>
        /// Verifies Add All CanResolve matches Resolve emptiness when policy is off.
        /// </summary>
        [Fact]
        public void CanResolveFromCurrentFolder_BothPolicyOff_IsFalse()
        {
            Assert.False(
                RenameListAddSources.CanResolveFromCurrentFolder(
                    canAddAllToCurrentFolder: true,
                    addFiles: false,
                    addFolders: false
                )
            );
            Assert.Empty(
                RenameListAddSources.ResolveSourcesFromCurrentFolder(
                    currentPath: _dir,
                    mask: "*",
                    canAddAllToCurrentFolder: true,
                    addFiles: false,
                    addFolders: false
                )
            );
        }
    }
}
