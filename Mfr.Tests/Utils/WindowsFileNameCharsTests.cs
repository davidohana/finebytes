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
    }
}
