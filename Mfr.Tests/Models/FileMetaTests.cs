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

        /// <summary>
        /// Verifies absolute full-path writes replace directory, prefix, and extension.
        /// </summary>
        [Fact]
        public void SetFromAbsoluteFullPath_updates_directory_and_name()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("old"),
                prefix: "old",
                extension: "bak"
            );

            var next = TestPaths.Absolute("album", "track.mp3");
            meta.SetFromAbsoluteFullPath(next);

            Assert.Equal(TestPaths.Absolute("album"), meta.DirectoryPath);
            Assert.Equal("track", meta.Prefix);
            Assert.Equal("mp3", meta.Extension);
        }

        /// <summary>
        /// Verifies Windows-illegal path characters are rejected on any host OS.
        /// </summary>
        [Fact]
        public void SetFromAbsoluteFullPath_rejects_windows_illegal_path_chars()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                prefix: "track",
                extension: "mp3"
            );

            var bad = TestPaths.Absolute("album|bad", "track.mp3");
            var ex = Assert.Throws<ArgumentException>(() => meta.SetFromAbsoluteFullPath(bad));
            Assert.Contains("invalid characters", ex.Message);
        }

        /// <summary>
        /// Verifies relative paths are rejected.
        /// </summary>
        [Fact]
        public void SetFromAbsoluteFullPath_rejects_relative_path()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                prefix: "track",
                extension: "mp3"
            );

            var ex = Assert.Throws<ArgumentException>(() => meta.SetFromAbsoluteFullPath("relative\\track.mp3"));
            Assert.Contains("fully qualified", ex.Message);
        }

        /// <summary>
        /// Verifies absolute directory writes replace <see cref="FileMeta.DirectoryPath"/>.
        /// </summary>
        [Fact]
        public void SetAbsoluteDirectoryPath_updates_directory()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("old"),
                prefix: "track",
                extension: "mp3"
            );

            var next = TestPaths.Absolute("new", "folder");
            meta.SetAbsoluteDirectoryPath(next);

            Assert.Equal(next, meta.DirectoryPath);
            Assert.Equal("track", meta.Prefix);
        }
    }
}
