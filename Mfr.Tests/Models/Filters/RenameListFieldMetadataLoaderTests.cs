using Mfr.Filters;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="RenameListFieldMetadataLoader"/>.
    /// </summary>
    public sealed class RenameListFieldMetadataLoaderTests
    {
        [Fact]
        public void Directory_rows_skip_disk_loads()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var imageKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            var mediaKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType");

            RenameListFieldMetadataLoader.TryEnsureLoaded(item, audioKey);
            RenameListFieldMetadataLoader.TryEnsureLoaded(item, imageKey);
            RenameListFieldMetadataLoader.TryEnsureLoaded(item, mediaKey);

            Assert.False(item.EmbeddedTagsLoadAttempted);
            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.False(item.MediaPropertiesLoadAttempted);
        }

        [Fact]
        public void Missing_file_does_not_throw_and_marks_load_attempted()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var imageKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            var mediaKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType");

            RenameListFieldMetadataLoader.TryEnsureLoaded(item, audioKey);
            RenameListFieldMetadataLoader.TryEnsureLoaded(item, imageKey);
            RenameListFieldMetadataLoader.TryEnsureLoaded(item, mediaKey);

            Assert.True(item.EmbeddedTagsLoadAttempted);
            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.True(item.MediaPropertiesLoadAttempted);
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, audioKey));
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, imageKey));
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, mediaKey));
        }

        [Fact]
        public void Loads_embedded_tags_from_disk_once()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "tagged.wav");
                TaggedMinimalWav.WriteTagged(path, title: "DiskTitle", album: "SnapshotAlbum");
                var item = _UnmarkedItem(path);
                var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

                Assert.False(item.EmbeddedTagsLoadAttempted);
                RenameListFieldMetadataLoader.TryEnsureLoaded(item, titleKey);

                Assert.True(item.EmbeddedTagsLoadAttempted);
                Assert.Equal("DiskTitle", RenameListFieldCatalog.Resolve(item, titleKey));

                item.Original.AudioTagOverlay.ClearAllBlocks();
                RenameListFieldMetadataLoader.TryEnsureLoaded(item, titleKey);
                Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, titleKey));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Loads_image_properties_from_exif_fixture()
        {
            var item = _UnmarkedFixtureItem("tiny-exif.jpeg");
            var makeKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");

            Assert.False(item.ImagePropertiesLoadAttempted);
            RenameListFieldMetadataLoader.TryEnsureLoaded(item, makeKey);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.Equal("Canon", RenameListFieldCatalog.Resolve(item, makeKey));
        }

        [Fact]
        public void Loads_media_properties_from_disk_and_marks_tags_as_sibling()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "sample.mp3");
                File.Copy(
                    Path.Combine(AppContext.BaseDirectory, "Fixtures", "l3-compl-cut.mp3"),
                    path,
                    overwrite: true
                );
                var item = _UnmarkedItem(path);
                var layerKey = RenameListFieldKey.Original(MpegRenameListFields.Group, "Layer");

                Assert.False(item.MediaPropertiesLoadAttempted);
                Assert.False(item.EmbeddedTagsLoadAttempted);
                RenameListFieldMetadataLoader.TryEnsureLoaded(item, layerKey);

                Assert.True(item.MediaPropertiesLoadAttempted);
                Assert.True(item.EmbeddedTagsLoadAttempted);
                Assert.Equal("III", RenameListFieldCatalog.Resolve(item, layerKey));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Jpeg_file_with_audio_column_does_not_throw()
        {
            var item = _UnmarkedFixtureItem("tiny.jpeg");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListFieldMetadataLoader.TryEnsureLoaded(item, titleKey);

            Assert.True(item.EmbeddedTagsLoadAttempted);
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, titleKey));
        }

        private static RenameItem _UnmarkedFixtureItem(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");
            return _UnmarkedItem(Path.GetFullPath(fixturePath));
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
