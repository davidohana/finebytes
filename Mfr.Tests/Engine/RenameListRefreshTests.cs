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

            var renameList = new RenameList(includeHidden: true);
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

            var renameList = new RenameList(includeHidden: true);
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

            var renameList = new RenameList(includeHidden: true);
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

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            var storedPath = item.Original.FullPath;

            File.Delete(path);
            renameList.RefreshOriginals();

            Assert.Equal(storedPath, item.Original.FullPath);
            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));
        }
    }
}
