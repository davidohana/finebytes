using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Audio
{
    /// <summary>
    /// Tests for <see cref="TagRemoverFilter"/>: full strip and selective tag-block removal.
    /// </summary>
    public sealed class TagRemoverFilterTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies <see cref="BaseFilter.Type"/> matches the preset JSON discriminator.
        /// </summary>
        [Fact]
        public void Type_IsTagRemover()
        {
            var filter = _CreateAllFilter();
            Assert.Equal("TagRemover", filter.Type);
        }

        /// <summary>
        /// Verifies an empty block list with <c>all</c> false is rejected as a misconfigured preset.
        /// </summary>
        [Fact]
        public void Setup_EmptyBlocks_WithoutAll_ThrowsArgumentException()
        {
            var filter = new TagRemoverFilter(new TagRemoverOptions(Blocks: []));

            var ex = Assert.Throws<ArgumentException>(filter.Setup);
            Assert.Contains("at least one tag block type", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies <c>all: true</c> does not require <c>blocks</c>.
        /// </summary>
        [Fact]
        public void Setup_AllTrue_AllowsEmptyBlocks()
        {
            var filter = _CreateAllFilter();
            filter.Setup();
        }

        /// <summary>
        /// Verifies preview clears the overlay and sets the commit strip flag when <c>all</c> is true.
        /// </summary>
        [Fact]
        public void Apply_All_ClearsOverlay_And_SetsStripFlag_WhenReaderPresent()
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

            var item = new RenameItem(meta);
            item.MarkEmbeddedTagsLoadAttempted();
            var filter = _CreateAllFilter();
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
        public void Apply_All_AfterFormatterOnTitle_ClearsFormatterOverlay_And_SetsStripFlag()
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

            var item = new RenameItem(meta);
            item.MarkEmbeddedTagsLoadAttempted();
            var formatter = new FormatterFilter(
                Target: new SemanticAudioFieldTarget(SemanticAudioField.Title),
                Options: new FormatterOptions("Formatted"));
            var remover = _CreateAllFilter();
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
        public void Apply_All_AfterAudioTagSetter_ClearsSetterOverlay_And_SetsStripFlag()
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

            var item = new RenameItem(meta);
            item.MarkEmbeddedTagsLoadAttempted();
            var setter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "FromSetter")));
            var remover = _CreateAllFilter();
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
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);
            var filter = _CreateAllFilter();
            filter.Setup();

            Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
        }

        /// <summary>
        /// Verifies removing ID3v1 leaves the ID3v2 block, and the surviving block still drives the projection.
        /// </summary>
        [Fact]
        public void Apply_Mp3_RemovesId3v1_KeepsId3v2()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");
            var filter = _CreateBlocksFilter(AudioTagBlockKind.Id3v1);
            filter.Setup();

            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Id3v1);
            Assert.NotNull(item.Preview.AudioTagOverlay.Id3v2);
            Assert.Equal("FrameTitle", item.Preview.AudioTagOverlay.Semantic().Title);
            Assert.NotNull(item.Original.AudioTagOverlay.Id3v1);
            Assert.True(item.HasPreviewChanges());
        }

        /// <summary>
        /// Verifies selective removal never requests the nuclear strip reserved for <c>all: true</c>.
        /// </summary>
        [Fact]
        public void Apply_Mp3_DoesNotRequestStripAllOnCommit()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");
            var filter = _CreateBlocksFilter(AudioTagBlockKind.Id3v1);
            filter.Setup();

            filter.Apply(item);

            Assert.False(item.StripAllEmbeddedTagsOnCommit);
        }

        /// <summary>
        /// Verifies naming every supported block clears the overlay without the strip flag.
        /// </summary>
        [Fact]
        public void Apply_Mp3_RemovesBothId3Blocks()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");
            var filter = _CreateBlocksFilter(AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2);
            filter.Setup();

            filter.Apply(item);

            Assert.False(item.Preview.AudioTagOverlay.HasAnyBlock());
            Assert.False(item.StripAllEmbeddedTagsOnCommit);
        }

        /// <summary>
        /// Verifies a supported block the file does not carry is a no-op rather than an error.
        /// </summary>
        [Fact]
        public void Apply_Mp3_WithoutId3v1_IsNoOp()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: null, id3v2Title: "FrameTitle");
            Assert.Null(item.Original.AudioTagOverlay.Id3v1);
            var filter = _CreateBlocksFilter(AudioTagBlockKind.Id3v1);
            filter.Setup();

            filter.Apply(item);

            Assert.Equal("FrameTitle", item.Preview.AudioTagOverlay.Semantic().Title);
            Assert.False(item.HasPreviewChanges());
        }

        /// <summary>
        /// Verifies removing a block the container cannot hold is a loud error, per the container policy.
        /// </summary>
        [Fact]
        public void Apply_Id3v2OnFlac_ThrowsNotSupported()
        {
            var item = _CreateFlacItem();
            var filter = _CreateBlocksFilter(AudioTagBlockKind.Id3v2);
            filter.Setup();

            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("FLAC", ex.Message, StringComparison.Ordinal);
        }

        private static TagRemoverFilter _CreateAllFilter()
        {
            return new TagRemoverFilter(new TagRemoverOptions(All: true));
        }

        private static TagRemoverFilter _CreateBlocksFilter(params AudioTagBlockKind[] blocks)
        {
            return new TagRemoverFilter(new TagRemoverOptions(Blocks: blocks));
        }

        private RenameItem _CreateTaggedMp3Item(string? id3v1Title, string? id3v2Title)
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("selective.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v1Title: id3v1Title, id3v2Title: id3v2Title);
            return _CreateRenameItemFor(path);
        }

        private RenameItem _CreateFlacItem()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("selective.flac");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "metaflac.flac");
            File.Copy(fixturePath, path, overwrite: false);
            return _CreateRenameItemFor(path);
        }

        private static RenameItem _CreateRenameItemFor(string absolutePath)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetDirectoryName(absolutePath)!,
                prefix: Path.GetFileNameWithoutExtension(absolutePath),
                extension: Path.GetExtension(absolutePath));

            return new RenameItem(meta);
        }
    }
}
