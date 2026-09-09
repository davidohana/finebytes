namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="FilterTargetText"/>.
    /// </summary>
    public sealed class FilterTargetTextTests
    {
        /// <summary>
        /// Verifies prefix / extension / full-name targets read from a snapshot.
        /// </summary>
        [Fact]
        public void TryGet_reads_file_name_targets()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                prefix: "track",
                extension: ".mp3"
            );

            Assert.True(FilterTargetText.TryGet(meta, new FilePrefixTarget(), out var prefix));
            Assert.Equal("track", prefix);
            Assert.True(FilterTargetText.TryGet(meta, new FileExtensionTarget(), out var extension));
            Assert.Equal("mp3", extension);
            Assert.True(FilterTargetText.TryGet(meta, new FileFullNameTarget(), out var fullName));
            Assert.Equal("track.mp3", fullName);
        }

        /// <summary>
        /// Verifies RenameItem overload uses Original (not Preview).
        /// </summary>
        [Fact]
        public void TryGet_rename_item_uses_original()
        {
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: TestPaths.Absolute("album"),
                    prefix: "original",
                    extension: ".txt"
                )
            );
            item.Preview.Prefix = "previewed";

            Assert.True(FilterTargetText.TryGet(item, new FilePrefixTarget(), out var text));
            Assert.Equal("original", text);
        }

        /// <summary>
        /// Verifies Extension apply-target returns empty when the snapshot has no extension.
        /// </summary>
        [Fact]
        public void TryGet_extension_empty_when_missing()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                prefix: "track",
                extension: ""
            );

            Assert.True(FilterTargetText.TryGet(meta, new FileExtensionTarget(), out var extension));
            Assert.Equal(string.Empty, extension);
        }
    }
}
