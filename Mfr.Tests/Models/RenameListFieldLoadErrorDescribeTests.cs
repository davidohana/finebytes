using Mfr.Filters;
using Mfr.Models.Rename;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Jpeg;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for user-facing Rename List metadata load-error copy.
    /// </summary>
    public sealed class RenameListFieldLoadErrorDescribeTests
    {
        /// <summary>
        /// Verifies audio/media load failures use a generic explanation, not format-specific copy.
        /// </summary>
        [Fact]
        public void DescribeFieldLoadError_uses_audio_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Music\PLAYLIST.M3U");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            item.SetTagLibMetadataLoadError(new InvalidOperationException(@"D:\Music\PLAYLIST.M3U (taglib/m3u)"));

            var explanation = RenameListFieldCatalog.DescribeFieldLoadError(item, titleKey);

            Assert.Equal("This file could not be read as audio or media metadata.", explanation);
        }

        /// <summary>
        /// Verifies I/O failures use a generic missing-file explanation.
        /// </summary>
        [Fact]
        public void DescribeFieldLoadError_uses_io_message_for_missing_files()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            item.SetTagLibMetadataLoadError(new FileNotFoundException("missing"));

            var explanation = RenameListFieldCatalog.DescribeFieldLoadError(item, titleKey);

            Assert.Equal("The file is missing or could not be opened.", explanation);
        }

        /// <summary>
        /// Verifies image load failures use the image-bucket explanation.
        /// </summary>
        [Fact]
        public void DescribeFieldLoadError_uses_image_bucket_message()
        {
            var item = _UnmarkedItem(@"D:\Music\notes.txt");
            var makeKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            item.SetImagePropertiesLoadError(new InvalidOperationException("Cannot read image properties"));

            var explanation = RenameListFieldCatalog.DescribeFieldLoadError(item, makeKey);

            Assert.Equal("This file could not be read as image or EXIF metadata.", explanation);
        }

        /// <summary>
        /// Verifies image-only columns are not treated as audio/media because of overlapping flag bits.
        /// </summary>
        [Fact]
        public void ImageProperties_flag_does_not_include_audio_or_media()
        {
            var image = RenameListMetadataRequirement.ImageProperties;
            Assert.False(image.HasFlag(RenameListMetadataRequirement.EmbeddedAudioTags));
            Assert.False(image.HasFlag(RenameListMetadataRequirement.MediaProperties));
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

                Assert.NotNull(item.GetTagLibMetadataLoadError());
                var error = item.GetTagLibMetadataLoadError()!;
                Assert.Contains("taglib", error.Message, StringComparison.OrdinalIgnoreCase);

                var explanation = RenameListFieldCatalog.DescribeFieldLoadError(item, titleKey);
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
                prefix: Path.GetFileNameWithoutExtension(fullPath),
                extension: Path.GetExtension(fullPath),
                fileSize: File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0
            );

            return new RenameItem(meta);
        }
    }
}
