using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
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

            Assert.Equal("song", token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-name", token.Names);
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FileNameToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x")(item));
            Assert.Contains("file-name", ex.Message);
        }
    }
}
