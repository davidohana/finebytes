using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="EpubFileReader"/>.
    /// </summary>
    public sealed class EpubFileReaderTests
    {
        [Fact]
        public void Read_InfoFixture_FillsFields()
        {
            var path = FixturePaths.Require("tiny-info.epub");

            var epub = EpubFileReader.Read(path);

            Assert.Equal("Sample EPUB Title", epub.Title);
            Assert.Equal("Sample EPUB Creator", epub.Creator);
            Assert.Equal("Sample EPUB Publisher", epub.Publisher);
            Assert.Equal("en", epub.Language);
            Assert.Equal("2024-03-15", epub.Date);
            // Fixture lists isbn before uid so this asserts unique-identifier preference, not list order.
            Assert.Equal("urn:uuid:sample-unique-id", epub.Identifier);
            Assert.Equal("Sample EPUB Subject", epub.Subject);
            Assert.Equal("Sample EPUB Description", epub.Description);
        }

        [Fact]
        public void Read_EmptyInfoFixture_LeavesFieldsEmpty()
        {
            var path = FixturePaths.Require("tiny-empty-info.epub");

            var epub = EpubFileReader.Read(path);

            Assert.Null(epub.Title);
            Assert.Null(epub.Creator);
            Assert.Null(epub.Publisher);
            Assert.Null(epub.Language);
            Assert.Null(epub.Date);
            Assert.Null(epub.Identifier);
            Assert.Null(epub.Subject);
            Assert.Null(epub.Description);
        }

        [Fact]
        public void Read_NonEpub_Throws()
        {
            var path = FixturePaths.Require("tiny.jpeg");

            // Non-ZIP bytes fail in the archive layer before VersOne raises Epub* types.
            Assert.Throws<InvalidDataException>(() => EpubFileReader.Read(path));
        }

        [Fact]
        public void Read_MissingPath_ThrowsArgumentException()
        {
            var missing = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.epub");
            var ex = Assert.Throws<ArgumentException>(() => EpubFileReader.Read(missing));
            Assert.Contains("exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
