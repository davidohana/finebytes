using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FullPathToken"/>.
    /// </summary>
    public sealed class FullPathTokenTests
    {
        /// <summary>
        /// Verifies the token returns the underlying file's full path string.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsFullPath()
        {
            var token = new FullPathToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album");

            Assert.Equal(item.Original.FullPath, token.Resolve(arg: "", item: item));
        }
    }
}
