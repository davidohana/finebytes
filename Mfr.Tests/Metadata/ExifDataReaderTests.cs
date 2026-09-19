using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="ExifDataReader"/>.
    /// </summary>
    public sealed class ExifDataReaderTests
    {
        [Fact]
        public void Read_ExifJpegFixture_MapsSemanticFieldsAndTagToDescription()
        {
            var path = FixturePaths.Require("tiny-exif.jpeg");

            var exif = ExifDataReader.Read(path);

            Assert.Equal("Canon", exif.Make);
            Assert.Equal("EOS 5D", exif.Model);
            Assert.Equal(new DateTime(2020, 5, 15, 14, 30, 0, DateTimeKind.Unspecified), exif.DateTaken);
            Assert.Equal(DateTimeKind.Unspecified, exif.DateTaken?.Kind);
            Assert.False(string.IsNullOrWhiteSpace(exif.Exposure));
            Assert.False(string.IsNullOrWhiteSpace(exif.FNumber));
            Assert.Contains("sec", exif.Exposure, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("8", exif.FNumber, StringComparison.Ordinal);

            Assert.Equal("Canon", exif.TagToDescription["Exif/Make"]);
            Assert.Equal("Canon", exif.TagToDescription["Exif/271"]);
            Assert.True(exif.TagToDescription.ContainsKey("ExifSub/Date/Time Original"));
            Assert.True(exif.TagToDescription.ContainsKey("ExifSub/36867"));
            Assert.Null(exif.GpsLatitude);
            Assert.Null(exif.GpsLongitude);
        }

        [Fact]
        public void Read_TinyJpegWithoutExif_EmptySnapshot()
        {
            var path = FixturePaths.Require("tiny.jpeg");

            var exif = ExifDataReader.Read(path);

            Assert.Null(exif.Make);
            Assert.Null(exif.Model);
            Assert.Null(exif.DateTaken);
            Assert.Null(exif.Exposure);
            Assert.Null(exif.GpsLatitude);
            Assert.Null(exif.GpsLongitude);
            Assert.Empty(exif.TagToDescription);
        }

        [Fact]
        public void Read_GpsJpegFixture_MapsTypedLatLonAndGpsTagDescriptions()
        {
            var path = FixturePaths.Require("tiny-gps.jpeg");

            var exif = ExifDataReader.Read(path);

            Assert.Equal(32.823057, exif.GpsLatitude!.Value, precision: 6);
            Assert.Equal(34.971542, exif.GpsLongitude!.Value, precision: 6);
            Assert.True(exif.TagToDescription.ContainsKey("GPS/GPS Latitude"));
            Assert.True(exif.TagToDescription.ContainsKey("GPS/2"));
        }

        [Fact]
        public void Read_PngFixture_EmptyExifStillMapsViaAllowlist()
        {
            var path = FixturePaths.Require("tiny.png");

            var exif = ExifDataReader.Read(path);

            Assert.Null(exif.Make);
            Assert.Null(exif.DateTaken);
            Assert.Empty(exif.TagToDescription);
        }

        [Fact]
        public void Read_HeifFixtureWithoutExif_EmptySnapshot()
        {
            var path = FixturePaths.Require("tiny.heic");

            var exif = ExifDataReader.Read(path);

            Assert.Null(exif.Make);
            Assert.Null(exif.Model);
            Assert.Null(exif.DateTaken);
            Assert.Null(exif.Exposure);
            Assert.Empty(exif.TagToDescription);
        }

        [Fact]
        public void Read_HeifExifFixture_MapsExifTagToDescription()
        {
            var path = FixturePaths.Require("tiny-exif.heic");

            var exif = ExifDataReader.Read(path);

            Assert.Null(exif.Make);
            Assert.Null(exif.DateTaken);
            Assert.True(exif.TagToDescription.ContainsKey("Exif/Orientation"));
            Assert.False(string.IsNullOrWhiteSpace(exif.TagToDescription["Exif/Orientation"]));
        }

        [Fact]
        public void Read_WavFixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("minimal-silent.wav");

            var ex = Assert.Throws<InvalidOperationException>(() => ExifDataReader.Read(path));
            Assert.Contains("WAV", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Mp3Fixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("l3-compl-cut.mp3");

            var ex = Assert.Throws<InvalidOperationException>(() => ExifDataReader.Read(path));
            Assert.Contains("MP3", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
