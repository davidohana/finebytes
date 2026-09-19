namespace Mfr.Tests.Models.Media
{
    /// <summary>
    /// Unit tests for shared <see cref="ExifDataFormatting"/>.
    /// </summary>
    public sealed class ExifDataFormattingTests
    {
        [Fact]
        public void Format_make_and_gps_match_shared_helpers()
        {
            var exif = new ExifData
            {
                Make = "Canon",
                GpsLatitude = 32.823057,
                GpsLongitude = 34.971542,
            };

            Assert.Equal("Canon", ExifDataFormatting.Format(exif, ExifPropertyField.Make));
            Assert.Equal(
                ExifGpsFormatting.FormatCoordinate(exif.GpsLatitude),
                ExifDataFormatting.Format(exif, ExifPropertyField.GpsLatitude)
            );
            Assert.Equal(
                ExifGpsFormatting.FormatCoordinate(exif.GpsLongitude),
                ExifDataFormatting.Format(exif, ExifPropertyField.GpsLongitude)
            );
        }

        [Fact]
        public void Format_null_or_blank_returns_empty()
        {
            Assert.Equal(string.Empty, ExifDataFormatting.Format(null, ExifPropertyField.Model));
            Assert.Equal(string.Empty, ExifDataFormatting.Format(new ExifData(), ExifPropertyField.Model));
        }
    }
}
