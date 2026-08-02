using Mfr.Filters.Formatting.Tokens.FileProperties;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Tests for <see cref="FileCountToken"/>.
    /// </summary>
    public sealed class FileCountTokenTests
    {
        /// <summary>
        /// Verifies a file item reports the file count of its parent directory (subfolders ignored).
        /// </summary>
        [Fact]
        public void Resolve_FileItem_CountsFilesInParentDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");
                Directory.CreateDirectory(Path.Combine(tempDir, "sub"));

                var token = new FileCountToken();
                var item = FilterTestHelpers.CreateRenameItem(directory: tempDir);

                Assert.Equal("2", token.Compile(tokenArgs: "")(item));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies a folder item reports the file count inside itself (not its parent).
        /// </summary>
        [Fact]
        public void Resolve_FolderItem_CountsFilesInsideFolder()
        {
            var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var folderPath = Path.Combine(parentDir, "Album");
            Directory.CreateDirectory(folderPath);
            try
            {
                File.WriteAllText(Path.Combine(parentDir, "sibling.txt"), "");
                File.WriteAllText(Path.Combine(folderPath, "track1.mp3"), "");
                File.WriteAllText(Path.Combine(folderPath, "track2.mp3"), "");
                Directory.CreateDirectory(Path.Combine(folderPath, "art"));

                var token = new FileCountToken();
                var item = FilterTestHelpers.CreateRenameItem(
                    prefix: "Album",
                    extension: "",
                    directory: parentDir,
                    attributes: FileAttributes.Directory);

                Assert.Equal("2", token.Compile(tokenArgs: "")(item));
            }
            finally
            {
                Directory.Delete(parentDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies an empty directory reports a count of 0.
        /// </summary>
        [Fact]
        public void Resolve_EmptyDirectory_ReturnsZero()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var token = new FileCountToken();
                var item = FilterTestHelpers.CreateRenameItem(directory: tempDir);

                Assert.Equal("0", token.Compile(tokenArgs: "")(item));
            }
            finally
            {
                Directory.Delete(tempDir);
            }
        }

        /// <summary>
        /// Verifies a missing directory yields an empty string instead of throwing.
        /// </summary>
        [Fact]
        public void Resolve_NonExistentDirectory_ReturnsEmpty()
        {
            var token = new FileCountToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\DoesNotExist\Never");

            Assert.Equal(string.Empty, token.Compile(tokenArgs: "")(item));
        }
    }
}
