namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Original Refresh (re-read disk fields) for <see cref="RenameList"/>.
    /// </summary>
    public sealed class RenameListRefreshTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a temp root for refresh tests.
        /// </summary>
        public RenameListRefreshTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mfr-refresh-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        /// <summary>
        /// Verifies RefreshOriginals updates stat fields after the file changes on disk.
        /// </summary>
        [Fact]
        public void RefreshOriginals_Updates_LastWriteTime_And_Size()
        {
            var path = Path.Combine(_tempRoot, "refresh.txt");
            File.WriteAllText(path, "old");

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            var beforeWrite = item.Original.LastWriteTime;
            var beforeSize = item.Original.FileSize;

            File.WriteAllText(path, "much longer payload");
            File.SetLastWriteTime(path, beforeWrite.AddHours(1));

            renameList.RefreshOriginals();

            Assert.True(item.Original.LastWriteTime > beforeWrite);
            Assert.NotEqual(beforeSize, item.Original.FileSize);
            Assert.False(item.TagLibLoadAttempted);
        }

        /// <summary>
        /// Verifies RefreshOriginals clears metadata caches so hydrate reads updated tags.
        /// </summary>
        [Fact]
        public void RefreshOriginals_Clears_Metadata_And_Reloads_Updated_Tags()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "Before", album: null);

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);
            Assert.Equal("Before", item.Original.AudioTagOverlay.Semantic().Title);

            TaggedMinimalWav.WriteTagged(path, title: "After", album: null);

            renameList.RefreshOriginals();
            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.TagLibMetadataLoadError);

            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);
            Assert.Equal("After", item.Original.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies RefreshOriginals clears a stored load error so a fixed file can hydrate again.
        /// </summary>
        [Fact]
        public void RefreshOriginals_Clears_Load_Error_Then_Hydrate_Succeeds()
        {
            var path = Path.Combine(_tempRoot, "missing.wav");
            File.WriteAllText(path, "not audio");

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);
            Assert.NotNull(item.TagLibMetadataLoadError);

            TaggedMinimalWav.WriteTagged(path, title: "Fixed", album: null);

            renameList.RefreshOriginals();
            Assert.Null(item.TagLibMetadataLoadError);

            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.Equal("Fixed", item.Original.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies RefreshOriginals keeps the stored path when the file is gone and still clears caches.
        /// </summary>
        [Fact]
        public void RefreshOriginals_Missing_Path_Keeps_Stored_Path_And_Clears_Caches()
        {
            var path = Path.Combine(_tempRoot, "gone.txt");
            File.WriteAllText(path, "old");

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            var storedPath = item.Original.FullPath;

            File.Delete(path);
            Assert.False(RenameListDiskPaths.IsMissingFromDisk(item));

            renameList.RefreshOriginals();

            Assert.Equal(storedPath, item.Original.FullPath);
            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));
        }

        /// <summary>
        /// Verifies RefreshOriginals clears the missing snapshot when the path exists again.
        /// </summary>
        [Fact]
        public void RefreshOriginals_Restored_Path_Clears_Missing_Flag()
        {
            var path = Path.Combine(_tempRoot, "back.txt");
            File.WriteAllText(path, "old");

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            File.Delete(path);
            renameList.RefreshOriginals();
            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));

            File.WriteAllText(path, "new");
            renameList.RefreshOriginals();

            Assert.False(RenameListDiskPaths.IsMissingFromDisk(item));
            Assert.Equal("new".Length, item.Original.FileSize);
        }

        /// <summary>
        /// Verifies RefreshOriginals picks up Explorer-style case-only renames for siblings and their parent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Exercises the per-pass listing cache: both siblings resolve under one parent enumeration, and a
        /// second RefreshOriginals must not reuse a stale cache after another case-only rename.
        /// </para>
        /// </remarks>
        [Fact]
        public void RefreshOriginals_Updates_Leaf_And_Parent_Casing_For_Siblings()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var albumLower = Path.Combine(_tempRoot, "album");
            Directory.CreateDirectory(albumLower);
            var aLower = Path.Combine(albumLower, "a.txt");
            var bLower = Path.Combine(albumLower, "b.txt");
            File.WriteAllText(aLower, "a");
            File.WriteAllText(bLower, "b");

            var renameList = new RenameList();
            renameList.AddSources([aLower, bLower]);
            Assert.Equal(2, renameList.RenameItems.Count);

            _CaseOnlyRenameFile(aLower, Path.Combine(albumLower, "A.TXT"));
            _CaseOnlyRenameFile(bLower, Path.Combine(albumLower, "B.TXT"));
            var albumUpper = Path.Combine(_tempRoot, "ALBUM");
            _CaseOnlyRenameDirectory(albumLower, albumUpper);

            renameList.RefreshOriginals();

            Assert.Equal(Path.Combine(albumUpper, "A.TXT"), renameList.RenameItems[0].Original.FullPath);
            Assert.Equal(Path.Combine(albumUpper, "B.TXT"), renameList.RenameItems[1].Original.FullPath);

            var aMixed = Path.Combine(albumUpper, "a.Txt");
            var bMixed = Path.Combine(albumUpper, "b.Txt");
            _CaseOnlyRenameFile(Path.Combine(albumUpper, "A.TXT"), aMixed);
            _CaseOnlyRenameFile(Path.Combine(albumUpper, "B.TXT"), bMixed);

            renameList.RefreshOriginals();

            Assert.Equal(aMixed, renameList.RenameItems[0].Original.FullPath);
            Assert.Equal(bMixed, renameList.RenameItems[1].Original.FullPath);
        }

        private static void _CaseOnlyRenameFile(string fromPath, string toPath)
        {
            var tempPath = Path.Combine(
                Path.GetDirectoryName(fromPath)!,
                Path.GetFileName(fromPath) + ".mfrtmp-" + Guid.NewGuid().ToString("N")
            );
            File.Move(fromPath, tempPath);
            File.Move(tempPath, toPath);
        }

        private static void _CaseOnlyRenameDirectory(string fromPath, string toPath)
        {
            var tempPath = Path.Combine(
                Path.GetDirectoryName(fromPath)!,
                Path.GetFileName(fromPath) + ".mfrtmp-" + Guid.NewGuid().ToString("N")
            );
            Directory.Move(fromPath, tempPath);
            Directory.Move(tempPath, toPath);
        }
    }
}
