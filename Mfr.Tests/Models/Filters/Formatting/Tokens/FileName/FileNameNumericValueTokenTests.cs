using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileNameNumericValueToken"/>.
    /// </summary>
    public sealed class FileNameNumericValueTokenTests
    {
        /// <summary>
        /// Verifies the first digit run is returned with leading zeros stripped.
        /// </summary>
        [Theory]
        [InlineData("track01", "1")]
        [InlineData("02-song", "2")]
        [InlineData("abc123def456", "123")]
        [InlineData("file10", "10")]
        [InlineData("000", "0")]
        [InlineData("007", "7")]
        public void Resolve_FullNameWithDigits_ReturnsFirstNumber(string prefix, string expected)
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix);

            Assert.Equal(expected, token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-name-numeric-value", token.Names);
        }

        /// <summary>
        /// Verifies digits in the extension are found when the prefix has none (MFR7 FullName behavior).
        /// </summary>
        [Theory]
        [InlineData("song", ".mp3", "3")]
        [InlineData("clip", ".mp4", "4")]
        public void Resolve_DigitsOnlyInExtension_ReturnsExtensionNumber(string prefix, string extension, string expected)
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix, extension: extension);

            Assert.Equal(expected, token.Compile(tokenArgs: "")(item));
        }

        /// <summary>
        /// Verifies full names without digits expand to <c>0</c>.
        /// </summary>
        [Theory]
        [InlineData("track", ".txt")]
        [InlineData("", "")]
        [InlineData("no-digits", ".bak")]
        [InlineData("photo", ".jpg")]
        public void Resolve_FullNameWithoutDigits_ReturnsZero(string prefix, string extension)
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix, extension: extension);

            Assert.Equal("0", token.Compile(tokenArgs: "")(item));
        }

        /// <summary>
        /// Verifies the token follows preview when it diverges from original.
        /// </summary>
        [Fact]
        public void Resolve_UsesPreviewNotOriginal()
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track01");
            item.Preview.Prefix = "chapter07";

            Assert.Equal("7", token.Compile(tokenArgs: "")(item));
            Assert.Equal("track01", item.Original.Prefix);
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FileNameNumericValueToken();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x"));
            Assert.Contains("file-name-numeric-value", ex.Message);
        }
    }
}
