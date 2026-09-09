namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="FileMeta"/> extension storage and full-name composition.
    /// </summary>
    public sealed class FileMetaTests
    {
        /// <summary>
        /// Verifies <see cref="FileMeta.FullFileName"/> inserts a separator only when an extension is present.
        /// </summary>
        [Theory]
        [InlineData("track", "mp3", "track.mp3")]
        [InlineData("track", "", "track")]
        [InlineData("", "txt", ".txt")]
        [InlineData("", "", "")]
        public void FullFileName_composes_prefix_and_extension(string prefix, string extension, string expected)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                prefix: prefix,
                extension: extension
            );

            Assert.Equal(expected, meta.FullFileName);
        }

        /// <summary>
        /// Verifies path ingest strips the BCL leading-dot form for <see cref="FileMeta.Extension"/> storage.
        /// </summary>
        [Theory]
        [InlineData("track.mp3", "mp3")]
        [InlineData("track", "")]
        [InlineData("archive.tar.gz", "gz")]
        public void ExtensionWithoutDot_strips_path_get_extension_dot(string pathOrFileName, string expected)
        {
            Assert.Equal(expected, FileMeta.ExtensionWithoutDot(pathOrFileName));
        }
    }
}
