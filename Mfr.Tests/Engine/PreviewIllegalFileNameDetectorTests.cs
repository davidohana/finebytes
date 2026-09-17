namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="PreviewIllegalFileNameDetector.MarkIllegalNames"/>.
    /// </summary>
    public sealed class PreviewIllegalFileNameDetectorTests
    {
        /// <summary>
        /// Verifies a PreviewOk item whose full file name has Windows-illegal characters is marked PreviewError.
        /// </summary>
        [Fact]
        public void Illegal_full_file_name_marks_preview_error_and_keeps_preview_name()
        {
            var item = _CreatePreviewOkItem(fileName: "0:00:44", extension: "txt");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("0:00:44", item.Preview.FileName);
            Assert.Equal("0:00:44.txt", item.Preview.FullFileName);
            Assert.Equal("Target name '0:00:44.txt' contains illegal characters: ':'.", item.PreviewError!.Message);
        }

        /// <summary>
        /// Verifies multiple distinct illegal characters are listed in the error.
        /// </summary>
        [Fact]
        public void Illegal_full_file_name_lists_each_distinct_illegal_char()
        {
            var item = _CreatePreviewOkItem(fileName: "a*:b", extension: "txt");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal("Target name 'a*:b.txt' contains illegal characters: '*' ':'.", item.PreviewError!.Message);
        }

        /// <summary>
        /// Verifies legal names stay PreviewOk.
        /// </summary>
        [Fact]
        public void Legal_full_file_name_is_unchanged()
        {
            var item = _CreatePreviewOkItem(fileName: "0-00-44", extension: "txt");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewOk, item.Status);
            Assert.Null(item.PreviewError);
        }

        /// <summary>
        /// Verifies an empty full file name is marked PreviewError (MFR7 empty-name parity).
        /// </summary>
        [Fact]
        public void Empty_full_file_name_marks_preview_error()
        {
            var item = _CreatePreviewOkItem(fileName: "", extension: "");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("Target name is empty.", item.PreviewError!.Message);
        }

        /// <summary>
        /// Verifies a trailing period on the full file name is marked PreviewError.
        /// </summary>
        [Fact]
        public void Trailing_period_marks_preview_error_and_keeps_preview_name()
        {
            var item = _CreatePreviewOkItem(fileName: "track.", extension: "");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("track.", item.Preview.FullFileName);
            Assert.Equal("Target name 'track.' ends with a space or period.", item.PreviewError!.Message);
        }

        /// <summary>
        /// Verifies a trailing space on the full file name is marked PreviewError.
        /// </summary>
        [Fact]
        public void Trailing_space_marks_preview_error_and_keeps_preview_name()
        {
            var item = _CreatePreviewOkItem(fileName: "track", extension: "txt ");

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("track.txt ", item.Preview.FullFileName);
            Assert.Equal("Target name 'track.txt ' ends with a space or period.", item.PreviewError!.Message);
        }

        /// <summary>
        /// Verifies items already in PreviewError are not overwritten.
        /// </summary>
        [Fact]
        public void Existing_preview_error_is_left_alone()
        {
            var item = _CreatePreviewOkItem(fileName: "0:00:44", extension: "txt");
            item.SetPreviewError(message: "preexisting", cause: null);

            PreviewIllegalFileNameDetector.MarkIllegalNames([item]);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.Equal("preexisting", item.PreviewError!.Message);
        }

        private static RenameItem _CreatePreviewOkItem(string fileName, string extension)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: "track",
                extension: "txt"
            );
            var item = new RenameItem(meta) { Status = RenameStatus.PreviewOk };
            item.Preview.FileName = fileName;
            item.Preview.Extension = extension;
            return item;
        }
    }
}
