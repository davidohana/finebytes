using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FullPathToken"/>.
    /// </summary>
    public sealed class FullPathTokenTests
    {
        /// <summary>
        /// Verifies the token returns the preview full path string.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsFullPath()
        {
            var token = new FullPathToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album");

            Assert.Equal(item.Preview.FullPath, token.Compile(tokenArgs: "")(item));
        }

        /// <summary>
        /// Verifies the token follows preview when it diverges from original.
        /// </summary>
        [Fact]
        public void Resolve_UsesPreviewNotOriginal()
        {
            var token = new FullPathToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album");
            item.Preview.DirectoryPath = @"D:\Staging";
            item.Preview.Prefix = "renamed";

            Assert.Equal(item.Preview.FullPath, token.Compile(tokenArgs: "")(item));
            Assert.NotEqual(item.Original.FullPath, item.Preview.FullPath);
        }
    }
}
