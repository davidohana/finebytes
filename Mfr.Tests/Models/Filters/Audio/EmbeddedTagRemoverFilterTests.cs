using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Models.Tags;

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
                renameListFolderSiblingCount: 1)
            {
                AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "PreservedForTest")
            };

            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));
            var filter = new EmbeddedTagRemoverFilter();
            filter.Setup();
            filter.Apply(item);

            Assert.True(item.StripAllEmbeddedTagsOnCommit);
            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Title);
            Assert.Equal("PreservedForTest", item.Original.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies apply after a title <see cref="FormatterFilter"/> clears the overlay and sets the strip flag.
        /// </summary>
        [Fact]
        public void Apply_AfterFormatterOnTitle_ClearsFormatterOverlay_And_SetsStripFlag()
        {
            var meta = new FileMeta(
                0,
                0,
                @"C:\Music",
                "x",
                ".wav",
                renameListTotalCount: 1,
                renameListFolderSiblingCount: 1)
            {
                AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Start")
            };

            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));
            var formatter = new FormatterFilter(
                Target: new AudioOverlayFieldTarget(AudioOverlayField.Title),
                Options: new FormatterOptions("Formatted"));
            var remover = new EmbeddedTagRemoverFilter();
            formatter.Setup();
            formatter.Apply(item);
            Assert.Equal("Formatted", item.Preview.AudioTagOverlay.Semantic().Title);

            remover.Setup();
            remover.Apply(item);

            Assert.True(item.StripAllEmbeddedTagsOnCommit);
            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies apply after <see cref="AudioTagSetterFilter"/> clears the setter overlay and sets the strip flag.
        /// </summary>
        [Fact]
        public void Apply_AfterAudioTagSetter_ClearsSetterOverlay_And_SetsStripFlag()
        {
            var meta = new FileMeta(
                0,
                0,
                @"C:\Music",
                "x",
                ".wav",
                renameListTotalCount: 1,
                renameListFolderSiblingCount: 1)
            {
                AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Disk")
            };

            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));
            var setter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "FromSetter")));
            var remover = new EmbeddedTagRemoverFilter();
            setter.Setup();
            setter.Apply(item);
            Assert.Equal("FromSetter", item.Preview.AudioTagOverlay.Semantic().Title);

            remover.Setup();
            remover.Apply(item);

            Assert.True(item.StripAllEmbeddedTagsOnCommit);
            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Title);
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
