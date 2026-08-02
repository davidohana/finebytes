using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileOrFolderToken"/>.
    /// </summary>
    public sealed class FileOrFolderTokenTests
    {
        /// <summary>
        /// Verifies a normal file row resolves to <c>File</c>.
        /// </summary>
        [Fact]
        public void Resolve_File_ReturnsFile()
        {
            var token = new FileOrFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Normal);

            Assert.Equal("File", token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-or-folder", token.Names);
        }

        /// <summary>
        /// Verifies a directory row resolves to <c>Folder</c>.
        /// </summary>
        [Fact]
        public void Resolve_Directory_ReturnsFolder()
        {
            var token = new FileOrFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Album",
                extension: "",
                attributes: FileAttributes.Directory);

            Assert.Equal("Folder", token.Compile(tokenArgs: "")(item));
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FileOrFolderToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x")(item));
            Assert.Contains("file-or-folder", ex.Message);
        }
    }
}
