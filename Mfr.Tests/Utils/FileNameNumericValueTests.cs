using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="FileNameNumericValue"/>.
    /// </summary>
    public sealed class FileNameNumericValueTests
    {
        /// <summary>
        /// Verifies MFR7-style first digit run extraction (max 10 digits, zeros stripped via parse).
        /// </summary>
        [Theory]
        [InlineData("track01.txt", "1")]
        [InlineData("02-song.mp3", "2")]
        [InlineData("abc123def456", "123")]
        [InlineData("000", "0")]
        [InlineData("007.wav", "7")]
        [InlineData("no-digits.bak", "0")]
        [InlineData("12345678901", "1234567890")]
        [InlineData("00000000001", "0")]
        public void Extract_matches_mfr7_first_numeric_run(string fullFileName, string expected)
        {
            Assert.Equal(expected, FileNameNumericValue.Extract(fullFileName));
        }
    }
}
