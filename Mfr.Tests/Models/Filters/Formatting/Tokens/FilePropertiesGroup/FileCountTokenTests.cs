using Mfr.Filters.Formatting.Tokens.FilePropertiesGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Tests for <see cref="FileCountToken"/>.
    /// </summary>
    public sealed class FileCountTokenTests
    {
        /// <summary>
        /// Verifies a directory with two files reports a count of 2.
        /// </summary>
        [Fact]
        public void Resolve_DirectoryWithFiles_ReturnsEntryCount()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");

                var token = new FileCountToken();
                var item = FilterTestHelpers.CreateRenameItem(directory: tempDir);

                Assert.Equal("2", token.Resolve(arg: "", item: item));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
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

                Assert.Equal("0", token.Resolve(arg: "", item: item));
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

            Assert.Equal(string.Empty, token.Resolve(arg: "", item: item));
        }
    }
}
