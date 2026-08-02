using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileNameLengthToken"/>.
    /// </summary>
    public sealed class FileNameLengthTokenTests
    {
        /// <summary>
        /// Verifies the token returns the character length of preview prefix plus extension.
        /// </summary>
        [Theory]
        [InlineData("song", ".mp3", "8")]
        [InlineData("", ".txt", "4")]
        [InlineData("a", "", "1")]
        [InlineData("", "", "0")]
        public void Resolve_ReturnsPreviewFullNameLength(string prefix, string extension, string expected)
        {
            var token = new FileNameLengthToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix, extension: extension);

            Assert.Equal(expected, token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-name-length", token.Names);
        }

        /// <summary>
        /// Verifies the length follows preview when it diverges from original.
        /// </summary>
        [Fact]
        public void Resolve_UsesPreviewNotOriginal()
        {
            var token = new FileNameLengthToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: "short", extension: ".mp3");
            item.Preview.Prefix = "much-longer-name";

            Assert.Equal("20", token.Compile(tokenArgs: "")(item));
            Assert.Equal(9, item.Original.Prefix.Length + item.Original.Extension.Length);
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FileNameLengthToken();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x"));
            Assert.Contains("file-name-length", ex.Message);
        }
    }
}
