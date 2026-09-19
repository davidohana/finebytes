namespace Mfr.Tests.Models.Media
{
    /// <summary>
    /// Tests for <see cref="ExifGpsFormatting"/>.
    /// </summary>
    public sealed class ExifGpsFormattingTests
    {
        [Theory]
        [InlineData(32.823057, "32.823057")]
        [InlineData(34.971542, "34.971542")]
        [InlineData(32.5, "32.5")]
        [InlineData(32.0, "32")]
        [InlineData(-34.971542, "-34.971542")]
        [InlineData(0.000001, "0.000001")]
        [InlineData(0.0, "0")]
        public void FormatCoordinate_InvariantCulture_TrimsTrailingZeros(double value, string expected)
        {
            Assert.Equal(expected, ExifGpsFormatting.FormatCoordinate(value));
        }

        [Fact]
        public void FormatCoordinate_Null_YieldsEmpty()
        {
            Assert.Equal(string.Empty, ExifGpsFormatting.FormatCoordinate(null));
        }

        [Fact]
        public void FormatCoordinate_RoundsBeyondSixDecimalPlaces()
        {
            Assert.Equal("32.823057", ExifGpsFormatting.FormatCoordinate(32.823057000000006));
        }
    }
}
