using Mfr.Filters.Formatting.Tokens.FileNameGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Tests for <see cref="FileNameToken"/>.
    /// </summary>
    public sealed class FileNameTokenTests
    {
        /// <summary>
        /// Verifies the token returns the original prefix and exposes its canonical name.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsOriginalPrefix()
        {
            var token = new FileNameToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: "song", extension: ".mp3");

            Assert.Equal("song", token.Resolve(arg: "", item: item));
            Assert.Contains("file-name", token.Names);
        }
    }
}
