using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileExtensionToken"/>.
    /// </summary>
    public sealed class FileExtensionTokenTests
    {
        /// <summary>
        /// Verifies the token returns the extension with its leading dot.
        /// </summary>
        [Fact]
        public void Resolve_ReturnsExtensionWithDot()
        {
            var token = new FileExtensionToken();
            var item = FilterTestHelpers.CreateRenameItem(extension: ".flac");

            Assert.Equal(".flac", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies the token registers under both its canonical name and the <c>ext</c> alias.
        /// </summary>
        [Fact]
        public void Names_IncludeCanonicalAndAlias()
        {
            var token = new FileExtensionToken();

            Assert.Contains("file-extension", token.Names);
            Assert.Contains("ext", token.Names);
        }
    }
}
