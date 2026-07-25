using Mfr.Filters.Audio;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Audio
{
    /// <summary>
    /// Tests for <see cref="EmbeddedTagTypeRemoverFilter"/>: preview-side selective tag-block removal.
    /// </summary>
    public sealed class EmbeddedTagTypeRemoverFilterTests : IDisposable
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
        public void Type_IsEmbeddedTagTypeRemover()
        {
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1);
            Assert.Equal("EmbeddedTagTypeRemover", filter.Type);
        }

        /// <summary>
        /// Verifies an empty block list is rejected as a misconfigured preset rather than running as a no-op.
        /// </summary>
        [Fact]
        public void Setup_EmptyBlocks_ThrowsArgumentException()
        {
            var filter = new EmbeddedTagTypeRemoverFilter(new EmbeddedTagTypeRemoverOptions([]));

            var ex = Assert.Throws<ArgumentException>(filter.Setup);
            Assert.Contains("at least one tag block type", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies removing ID3v1 leaves the ID3v2 block, and the surviving block still drives the projection.
        /// </summary>
        [Fact]
        public void Apply_Mp3_RemovesId3v1_KeepsId3v2()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1);
            filter.Setup();

            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Id3v1);
            Assert.NotNull(item.Preview.AudioTagOverlay.Id3v2);
            Assert.Equal("FrameTitle", item.Preview.AudioTagOverlay.Semantic().Title);
            Assert.NotNull(item.Original.AudioTagOverlay.Id3v1);
            Assert.True(item.HasPreviewChanges());
        }

        /// <summary>
        /// Verifies selective removal never requests the nuclear strip reserved for <see cref="EmbeddedTagRemoverFilter"/>.
        /// </summary>
        [Fact]
        public void Apply_Mp3_DoesNotRequestStripAllOnCommit()
        {
            var item = _CreateTaggedMp3Item(id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1);
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
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2);
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
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1);
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
            var filter = _CreateFilter(AudioTagBlockKind.Id3v2);
            filter.Setup();

            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("FLAC", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies directory rows fail during tag load like other embedded-tag filters.
        /// </summary>
        [Fact]
        public void Apply_DirectoryRow_ThrowsInvalidOperation()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);
            var filter = _CreateFilter(AudioTagBlockKind.Id3v1);
            filter.Setup();

            Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
        }

        private static EmbeddedTagTypeRemoverFilter _CreateFilter(params AudioTagBlockKind[] blocks)
        {
            return new EmbeddedTagTypeRemoverFilter(new EmbeddedTagTypeRemoverOptions(blocks));
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
