using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="ImageFileReader"/> one-open snapshot mapping.
    /// </summary>
    public sealed class ImageFileReaderTests
    {
        [Fact]
        public void Read_ExifJpegFixture_FillsImageAndExif()
        {
            var path = FixturePaths.Require("tiny-exif.jpeg");

            var snapshot = ImageFileReader.Read(path);

            Assert.Equal("JPEG", snapshot.Image.Format);
            Assert.Equal(8, snapshot.Image.Width);
            Assert.Equal(6, snapshot.Image.Height);
            Assert.Equal("Canon", snapshot.Exif.Make);
            Assert.Equal("EOS 5D", snapshot.Exif.Model);
            Assert.NotNull(snapshot.Exif.DateTaken);
        }

        [Fact]
        public void Read_TinyJpegWithoutExif_MapsImageAndEmptyExif()
        {
            var path = FixturePaths.Require("tiny.jpeg");

            var snapshot = ImageFileReader.Read(path);

            Assert.Equal("JPEG", snapshot.Image.Format);
            Assert.Equal(8, snapshot.Image.Width);
            Assert.Null(snapshot.Exif.Make);
            Assert.Null(snapshot.Exif.DateTaken);
            Assert.Empty(snapshot.Exif.TagToDescription);
        }

        [Fact]
        public void Read_PngFixture_MapsImageAndEmptyExif()
        {
            var path = FixturePaths.Require("tiny.png");

            var snapshot = ImageFileReader.Read(path);

            Assert.Equal("PNG", snapshot.Image.Format);
            Assert.Equal(4, snapshot.Image.Width);
            Assert.Null(snapshot.Exif.Make);
            Assert.Empty(snapshot.Exif.TagToDescription);
        }

        [Fact]
        public void Read_WavFixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("minimal-silent.wav");

            var ex = Assert.Throws<InvalidOperationException>(() => ImageFileReader.Read(path));
            Assert.Contains("WAV", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Mp3Fixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("l3-compl-cut.mp3");

            var ex = Assert.Throws<InvalidOperationException>(() => ImageFileReader.Read(path));
            Assert.Contains("MP3", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
