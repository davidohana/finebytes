using Mfr.Filters.Audio;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Audio
{
    /// <summary>
    /// Tests for <see cref="EmbeddedTagRemoverFilter"/>.
    /// </summary>
    public sealed class EmbeddedTagRemoverFilterTests
    {
        /// <summary>
        /// Verifies preview clears the overlay and sets the commit strip flag after tag hydration.
        /// </summary>
        [Fact]
        public void Apply_ClearsOverlay_And_SetsStripFlag_WhenReaderPresent()
        {
            var meta = new FileMeta(
                0,
                0,
                @"C:\Music",
                "x",
                ".wav",
                renameListTotalCount: 1,
                renameListFolderSiblingCount: 1);
            meta.AudioTagOverlay.Title = "PreservedForTest";

            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));
            var filter = new EmbeddedTagRemoverFilter();
            filter.Setup();
            filter.Apply(item);

            Assert.True(item.StripAllEmbeddedTagsOnCommit);
            Assert.Null(item.Preview.AudioTagOverlay.Title);
            Assert.Equal("PreservedForTest", item.Original.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies directory rows fail during tag load like other overlay filters.
        /// </summary>
        [Fact]
        public void Apply_DirectoryRow_ThrowsInvalidOperation()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                attributes: FileAttributes.Directory);
            var filter = new EmbeddedTagRemoverFilter();
            filter.Setup();

            Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
        }

        /// <summary>
        /// Verifies <see cref="BaseFilter.Type"/> matches preset JSON discriminator.
        /// </summary>
        [Fact]
        public void Type_IsEmbeddedTagRemover()
        {
            var filter = new EmbeddedTagRemoverFilter();
            Assert.Equal("EmbeddedTagRemover", filter.Type);
        }
    }
}
