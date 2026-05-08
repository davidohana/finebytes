using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FullNameToken"/>.
    /// </summary>
    public sealed class FullNameTokenTests
    {
        /// <summary>
        /// Verifies the token concatenates prefix and extension.
        /// </summary>
        [Fact]
        public void Resolve_ConcatenatesPrefixAndExtension()
        {
            var token = new FullNameToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track01", extension: ".mp3");

            Assert.Equal("track01.mp3", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies the result is just the prefix when extension is empty (typical for folder items).
        /// </summary>
        [Fact]
        public void Resolve_EmptyExtension_ReturnsPrefixOnly()
        {
            var token = new FullNameToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Albums", extension: "");

            Assert.Equal("Albums", token.Resolve(arg: "", item: item));
        }
    }
}
