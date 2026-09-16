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
        public void FullFileName_composes_file_name_and_extension(string fileName, string extension, string expected)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: fileName,
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
                fileName: "old",
                extension: "bak"
            );

            var next = TestPaths.Absolute("album", "track.mp3");
            meta.SetFromAbsoluteFullPath(next);

            Assert.Equal(TestPaths.Absolute("album"), meta.DirectoryPath);
            Assert.Equal("track", meta.FileName);
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
                fileName: "track",
                extension: "mp3"
            );

            var bad = TestPaths.Absolute("album|bad", "track.mp3");
            var ex = Assert.Throws<ArgumentException>(() => meta.SetFromAbsoluteFullPath(bad));
            Assert.Contains("invalid characters", ex.Message);
        }

        /// <summary>
        /// Verifies a name-illegal leaf is accepted on write so preview can display the attempted name.
        /// </summary>
        [Fact]
        public void SetFromAbsoluteFullPath_accepts_windows_illegal_file_name_leaf()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: "track",
                extension: "mp3"
            );

            // '*' is illegal in file names but not in Windows path-char validation.
            var bad = TestPaths.Absolute("album", "bad*name.txt");
            meta.SetFromAbsoluteFullPath(bad);

            Assert.Equal("bad*name", meta.FileName);
            Assert.Equal("txt", meta.Extension);
        }

        /// <summary>
        /// Verifies File Name / Extension / Full File Name writes accept Windows-illegal characters (validated at preview end).
        /// </summary>
        [Theory]
        [InlineData(typeof(FileNameTarget), "0:00:44", "0:00:44", "mp3")]
        [InlineData(typeof(FileNameTarget), "bad*name", "bad*name", "mp3")]
        [InlineData(typeof(FileExtensionTarget), "mp3:x", "track", "mp3:x")]
        [InlineData(typeof(FileFullNameTarget), "0:00:44.mp3", "0:00:44", "mp3")]
        [InlineData(typeof(FileFullNameTarget), "song?.mp3", "song?", "mp3")]
        public void SetTargetString_accepts_windows_illegal_file_name_chars(
            Type targetType,
            string value,
            string expectedFileName,
            string expectedExtension
        )
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: "track",
                extension: "mp3"
            );
            var target = (FilterTarget)Activator.CreateInstance(targetType)!;

            meta.SetTargetString(target, value);

            Assert.Equal(expectedFileName, meta.FileName);
            Assert.Equal(expectedExtension, meta.Extension);
        }

        /// <summary>
        /// Verifies legal File Name / Extension / Full File Name writes still assign.
        /// </summary>
        [Fact]
        public void SetTargetString_accepts_legal_file_name_segments()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: "track",
                extension: "mp3"
            );

            meta.SetTargetString(new FileNameTarget(), "0-00-44");
            Assert.Equal("0-00-44", meta.FileName);

            meta.SetTargetString(new FileExtensionTarget(), "flac");
            Assert.Equal("flac", meta.Extension);

            meta.SetTargetString(new FileFullNameTarget(), "song.wav");
            Assert.Equal("song", meta.FileName);
            Assert.Equal("wav", meta.Extension);
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
                fileName: "track",
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
                fileName: "track",
                extension: "mp3"
            );

            var next = TestPaths.Absolute("new", "folder");
            meta.SetAbsoluteDirectoryPath(next);

            Assert.Equal(next, meta.DirectoryPath);
            Assert.Equal("track", meta.FileName);
        }
    }
}
