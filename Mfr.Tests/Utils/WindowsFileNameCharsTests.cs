using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="WindowsFileNameChars"/>.
    /// </summary>
    public sealed class WindowsFileNameCharsTests
    {
        /// <summary>
        /// Verifies Windows-illegal name characters are detected on any host OS.
        /// </summary>
        [Theory]
        [InlineData("ok-name", false)]
        [InlineData("has:colon", true)]
        [InlineData("has|pipe", true)]
        [InlineData("has*star", true)]
        [InlineData("has?q", true)]
        [InlineData("has<lt", true)]
        [InlineData("has>gt", true)]
        [InlineData("has\"quote", true)]
        [InlineData("has\\slash", true)]
        [InlineData("has/fwd", true)]
        [InlineData("has\0null", true)]
        public void ContainsInvalid_name_chars(string value, bool expected)
        {
            Assert.Equal(expected, WindowsFileNameChars.ContainsInvalid(value));
        }

        /// <summary>
        /// Verifies Windows path validation allows separators and drive letters but rejects path-illegal chars.
        /// </summary>
        [Theory]
        [InlineData(@"C:\folder\file.txt", false)]
        [InlineData(@"C:\folder|bad\file.txt", true)]
        [InlineData("C:\\folder<bad\\file.txt", true)]
        [InlineData("ok*wild", false)]
        public void ContainsInvalidPath_path_chars(string path, bool expected)
        {
            Assert.Equal(expected, WindowsFileNameChars.ContainsInvalidPath(path));
        }

        /// <summary>
        /// Verifies distinct illegal characters are reported in first-seen order.
        /// </summary>
        [Fact]
        public void FindInvalid_returns_distinct_chars_in_first_seen_order()
        {
            Assert.Equal([':', '*', '?'], WindowsFileNameChars.FindInvalid("a:b*a:c?"));
            Assert.Empty(WindowsFileNameChars.FindInvalid("ok-name"));
        }

        /// <summary>
        /// Verifies illegal characters are formatted for preview/commit error text.
        /// </summary>
        [Fact]
        public void FormatInvalidForMessage_quotes_printable_and_codes_controls()
        {
            Assert.Equal("':' '*'", WindowsFileNameChars.FormatInvalidForMessage([':', '*']));
            Assert.Equal("U+0000", WindowsFileNameChars.FormatInvalidForMessage(['\0']));
            Assert.Equal(string.Empty, WindowsFileNameChars.FormatInvalidForMessage([]));
        }

        /// <summary>
        /// Verifies null inputs are rejected.
        /// </summary>
        [Fact]
        public void Public_apis_reject_null()
        {
            Assert.Throws<ArgumentNullException>(() => WindowsFileNameChars.ContainsInvalid(null!));
            Assert.Throws<ArgumentNullException>(() => WindowsFileNameChars.ContainsInvalidPath(null!));
            Assert.Throws<ArgumentNullException>(() => WindowsFileNameChars.FindInvalid(null!));
            Assert.Throws<ArgumentNullException>(() => WindowsFileNameChars.FormatInvalidForMessage(null!));
            Assert.Throws<ArgumentNullException>(() => WindowsFileNameChars.AddInvalidTo(null!));
        }
    }
}
