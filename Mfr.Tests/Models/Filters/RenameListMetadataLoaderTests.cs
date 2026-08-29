using Mfr.Filters;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="RenameListMetadataLoader"/>.
    /// </summary>
    public sealed class RenameListMetadataLoaderTests
    {
        [Fact]
        public void Directory_rows_skip_disk_loads()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var imageKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            var mediaKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType");

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, imageKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, mediaKey);

            Assert.False(item.TagLibLoadAttempted);
            Assert.False(item.ImagePropertiesLoadAttempted);
        }

        [Fact]
        public void TagLib_failure_on_audio_key_satisfies_media_requirement()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);

            Assert.True(item.TagLibLoadAttempted);
            Assert.NotNull(item.TagLibMetadataLoadError);
            Assert.True(
                RenameListMetadataLoader.IsRequirementSatisfied(item, RenameListMetadataRequirement.TagLib)
            );
        }

        [Fact]
        public void Missing_file_does_not_throw_and_marks_load_attempted()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var imageKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            var mediaKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType");

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, imageKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, mediaKey);

            Assert.True(item.TagLibLoadAttempted);
            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.NotNull(item.TagLibMetadataLoadError);
            Assert.NotNull(item.ImagePropertiesLoadError);
            Assert.Equal(RenameListFieldCatalog.FieldLoadErrorText, RenameListFieldCatalog.Resolve(item, audioKey));
            Assert.Equal(RenameListFieldCatalog.FieldLoadErrorText, RenameListFieldCatalog.Resolve(item, imageKey));
            Assert.Equal(RenameListFieldCatalog.FieldLoadErrorText, RenameListFieldCatalog.Resolve(item, mediaKey));
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

                Assert.False(item.TagLibLoadAttempted);
                RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);

                Assert.True(item.TagLibLoadAttempted);
                Assert.Equal("DiskTitle", RenameListFieldCatalog.Resolve(item, titleKey));

                item.Original.AudioTagOverlay.ClearAllBlocks();
                RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);
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
            RenameListMetadataLoader.TryEnsureLoaded(item, makeKey);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.Equal("Canon", RenameListFieldCatalog.Resolve(item, makeKey));
        }

        [Fact]
        public void Loads_media_properties_from_disk_and_fills_tags()
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

                Assert.False(item.TagLibLoadAttempted);
                RenameListMetadataLoader.TryEnsureLoaded(item, layerKey);

                Assert.True(item.TagLibLoadAttempted);
                Assert.Equal("III", RenameListFieldCatalog.Resolve(item, layerKey));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Jpeg_file_with_audio_column_does_not_throw_and_shows_empty_tags()
        {
            var item = _UnmarkedFixtureItem("tiny.jpeg");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);

            Assert.True(item.TagLibLoadAttempted);
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, titleKey));
        }

        [Fact]
        public void Clear_metadata_cache_clears_load_errors()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);
            Assert.NotNull(item.TagLibMetadataLoadError);

            item.ClearMetadataCaches();
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.False(item.TagLibLoadAttempted);
        }

        [Fact]
        public void IsRequirementSatisfied_false_until_load_attempted()
        {
            var item = _UnmarkedItem(@"C:\DoesNotExist\Never\missing.mp3");
            var requirement = RenameListMetadataRequirement.TagLib;

            Assert.False(RenameListMetadataLoader.IsRequirementSatisfied(item, requirement));

            RenameListMetadataLoader.TryEnsureLoaded(item, requirement);

            Assert.True(RenameListMetadataLoader.IsRequirementSatisfied(item, requirement));
        }

        [Fact]
        public void AnyItemNeedsLoad_false_when_all_rows_satisfied()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            item.MarkTagLibLoadAttempted();
            var requirement = RenameListMetadataRequirement.TagLib;

            Assert.False(RenameListMetadataLoader.AnyItemNeedsLoad([item], requirement));
        }

        [Fact]
        public void Combined_requirement_loads_each_flagged_bucket()
        {
            var item = _UnmarkedFixtureItem("tiny-exif.jpeg");
            var requirement =
                RenameListMetadataRequirement.TagLib | RenameListMetadataRequirement.ImageProperties;

            Assert.True(RenameListMetadataLoader.AnyItemNeedsLoad([item], requirement));
            RenameListMetadataLoader.TryEnsureLoaded(item, requirement);

            Assert.True(item.TagLibLoadAttempted);
            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.False(RenameListMetadataLoader.AnyItemNeedsLoad([item], requirement));
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
