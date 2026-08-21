using System.Globalization;
using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FullPathLengthToken"/>.
    /// </summary>
    public sealed class FullPathLengthTokenTests
    {
        /// <summary>
        /// Verifies the token returns the character length of the preview full path.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsPreviewFullPathLength()
        {
            var token = new FullPathLengthToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album"
            );

            Assert.Equal(
                item.Preview.FullPath.Length.ToString(CultureInfo.InvariantCulture),
                token.Compile(tokenArgs: "")(item)
            );
            Assert.Contains("full-path-length", token.Names);
        }

        /// <summary>
        /// Verifies the length follows preview when it diverges from original.
        /// </summary>
        [Fact]
        public void Resolve_UsesPreviewNotOriginal()
        {
            var token = new FullPathLengthToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album"
            );
            var originalLength = item.Original.FullPath.Length;
            item.Preview.DirectoryPath = @"D:\Music\Much\Deeper\Album";

            var expected = item.Preview.FullPath.Length.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(expected, token.Compile(tokenArgs: "")(item));
            Assert.NotEqual(originalLength, item.Preview.FullPath.Length);
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FullPathLengthToken();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x"));
            Assert.Contains("full-path-length", ex.Message);
        }
    }
}
