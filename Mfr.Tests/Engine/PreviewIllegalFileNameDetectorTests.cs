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
            Assert.Contains("illegal characters", item.PreviewError!.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("0:00:44.txt", item.PreviewError.Message, StringComparison.Ordinal);
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
