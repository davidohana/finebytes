using Mfr.Filters;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Pdf;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for user-facing Rename List metadata load-error copy.
    /// </summary>
    public sealed class RenameListMetadataLoadErrorsTests
    {
        /// <summary>
        /// Verifies audio/media load failures use a generic explanation, not format-specific copy.
        /// </summary>
        [Fact]
        public void DescribeLoadError_uses_audio_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Music\PLAYLIST.M3U");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            item.SetTagLibMetadataLoadError(new InvalidOperationException(@"D:\Music\PLAYLIST.M3U (taglib/m3u)"));

            var explanation = RenameListFieldCatalog.DescribeLoadError(item, titleKey);

            Assert.Equal("This file could not be read as audio or media metadata.", explanation);
        }

        /// <summary>
        /// Verifies I/O failures use a generic missing-file explanation.
        /// </summary>
        [Fact]
        public void DescribeLoadError_uses_io_message_for_missing_files()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            item.SetTagLibMetadataLoadError(new FileNotFoundException("missing"));

            var explanation = RenameListFieldCatalog.DescribeLoadError(item, titleKey);

            Assert.Equal("The file is missing or could not be opened.", explanation);
        }

        /// <summary>
        /// Verifies image load failures use the image-bucket explanation.
        /// </summary>
        [Fact]
        public void DescribeLoadError_uses_image_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Music\notes.txt");
            var makeKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            item.SetImagePropertiesLoadError(new InvalidOperationException("Cannot read image properties"));

            var explanation = RenameListFieldCatalog.DescribeLoadError(item, makeKey);

            Assert.Equal("This file could not be read as image or EXIF metadata.", explanation);
        }

        /// <summary>
        /// Verifies PDF load failures use the PDF-bucket explanation.
        /// </summary>
        [Fact]
        public void DescribeLoadError_uses_pdf_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Docs\notes.txt");
            var titleKey = RenameListFieldKey.Original(PdfRenameListFields.Group, PdfRenameListFields.Key.Title);
            item.SetPdfLoadError(new InvalidOperationException("Cannot read PDF Info"));

            var explanation = RenameListFieldCatalog.DescribeLoadError(item, titleKey);

            Assert.Equal("This file could not be read as PDF document Info.", explanation);
        }

        /// <summary>
        /// Verifies EPUB load failures use the EPUB-bucket explanation.
        /// </summary>
        [Fact]
        public void DescribeLoadError_uses_epub_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Docs\notes.txt");
            item.SetEpubLoadError(new InvalidOperationException("Cannot read EPUB Info"));

            var explanation = RenameListMetadataLoadErrors.DescribeUserMessage(
                item.EpubLoadError!,
                RenameListMetadataRequirement.Epub
            );

            Assert.Equal("This file could not be read as EPUB document Info.", explanation);
        }

        /// <summary>
        /// Verifies listing returns TagLib, image, PDF, and EPUB failures for one row.
        /// </summary>
        [Fact]
        public void ListLoadErrors_returns_taglib_image_pdf_and_epub_failures()
        {
            var item = _UnmarkedItem(@"D:\Music\PLAYLIST.M3U");
            item.SetTagLibMetadataLoadError(new InvalidOperationException(@"D:\Music\PLAYLIST.M3U (taglib/m3u)"));
            item.SetImagePropertiesLoadError(new InvalidOperationException("Cannot read image properties"));
            item.SetPdfLoadError(new InvalidOperationException("Cannot read PDF Info"));
            item.SetEpubLoadError(new InvalidOperationException("Cannot read EPUB Info"));

            Assert.True(RenameListFieldCatalog.HasAnyLoadError(item));
            var errors = RenameListFieldCatalog.ListLoadErrors(item);
            Assert.Equal(4, errors.Count);
            Assert.Equal("This file could not be read as audio or media metadata.", errors[0].UserExplanation);
            Assert.Contains("taglib/m3u", errors[0].TechnicalDetails, StringComparison.Ordinal);
            Assert.Equal("This file could not be read as image or EXIF metadata.", errors[1].UserExplanation);
            Assert.Equal("Cannot read image properties", errors[1].TechnicalDetails);
            Assert.Equal("This file could not be read as PDF document Info.", errors[2].UserExplanation);
            Assert.Equal("Cannot read PDF Info", errors[2].TechnicalDetails);
            Assert.Equal("This file could not be read as EPUB document Info.", errors[3].UserExplanation);
            Assert.Equal("Cannot read EPUB Info", errors[3].TechnicalDetails);
        }

        /// <summary>
        /// Verifies real TagLib failures store the raw message for technical details.
        /// </summary>
        [Fact]
        public void Loader_stores_taglib_message_for_m3u_playlist()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "tracks.m3u");
                File.WriteAllText(path, "#EXTM3U\nsong.mp3\n");
                var item = _UnmarkedItem(path);
                var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

                RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);

                Assert.NotNull(item.TagLibMetadataLoadError);
                var error = item.TagLibMetadataLoadError;
                Assert.Contains("taglib", error.Message, StringComparison.OrdinalIgnoreCase);

                var explanation = RenameListFieldCatalog.DescribeLoadError(item, titleKey);
                Assert.Equal("This file could not be read as audio or media metadata.", explanation);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        private static RenameItem _UnmarkedItem(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath)!;
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                fileName: Path.GetFileNameWithoutExtension(fullPath),
                extension: FileMeta.ExtensionWithoutDot(fullPath),
                fileSize: File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0
            );

            return new RenameItem(meta);
        }
    }
}
