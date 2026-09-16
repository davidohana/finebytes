using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileNameToken"/>.
    /// </summary>
    public sealed class FileNameTokenTests
    {
        /// <summary>
        /// Verifies the token returns the preview file name and exposes its canonical name.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsPreviewPrefix()
        {
            var token = new FileNameToken();
            var item = FilterTestHelpers.CreateRenameItem(fileName: "song", extension: "mp3");

            Assert.Equal("song", token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-name", token.Names);
        }

        /// <summary>
        /// Verifies the token follows preview when it diverges from original.
        /// </summary>
        [Fact]
        public void Resolve_UsesPreviewNotOriginal()
        {
            var token = new FileNameToken();
            var item = FilterTestHelpers.CreateRenameItem(fileName: "song", extension: "mp3");
            item.Preview.FileName = "renamed";

            Assert.Equal("renamed", token.Compile(tokenArgs: "")(item));
            Assert.Equal("song", item.Original.FileName);
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
