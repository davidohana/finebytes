using Mfr.Filters;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mp3;
using Mfr.Models.RenameList.Fields.Pdf;

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
            RenameListMetadataLoader.TryEnsureLoaded(
                item,
                RenameListFieldKey.Original(PdfRenameListFields.Group, PdfRenameListFields.Key.Title)
            );
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Epub);
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Office);

            Assert.False(item.TagLibLoadAttempted);
            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.False(item.PdfLoadAttempted);
            Assert.False(item.EpubLoadAttempted);
            Assert.False(item.OfficeLoadAttempted);
        }

        [Fact]
        public void TagLib_failure_on_audio_key_satisfies_media_requirement()
        {
            var item = RenameItemFixtures.UnmarkedFromPath(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);

            Assert.True(item.TagLibLoadAttempted);
            Assert.NotNull(item.TagLibMetadataLoadError);
            Assert.True(RenameListMetadataLoader.IsRequirementSatisfied(item, RenameListMetadataRequirement.TagLib));
        }

        [Fact]
        public void Missing_file_does_not_throw_and_marks_load_attempted()
        {
            var item = RenameItemFixtures.UnmarkedFromPath(@"C:\DoesNotExist\Never\missing.mp3");
            var audioKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var imageKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");
            var mediaKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType");
            var pdfKey = RenameListFieldKey.Original(PdfRenameListFields.Group, PdfRenameListFields.Key.Title);

            RenameListMetadataLoader.TryEnsureLoaded(item, audioKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, imageKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, mediaKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, pdfKey);
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Epub);
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Office);

            Assert.True(item.TagLibLoadAttempted);
            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.True(item.PdfLoadAttempted);
            Assert.True(item.EpubLoadAttempted);
            Assert.True(item.OfficeLoadAttempted);
            Assert.NotNull(item.TagLibMetadataLoadError);
            Assert.NotNull(item.ImagePropertiesLoadError);
            Assert.NotNull(item.PdfLoadError);
            Assert.NotNull(item.EpubLoadError);
            Assert.NotNull(item.OfficeLoadError);
            Assert.Equal(RenameListFieldCatalog.LoadErrorText, RenameListFieldCatalog.Resolve(item, audioKey));
            Assert.Equal(RenameListFieldCatalog.LoadErrorText, RenameListFieldCatalog.Resolve(item, imageKey));
            Assert.Equal(RenameListFieldCatalog.LoadErrorText, RenameListFieldCatalog.Resolve(item, mediaKey));
            Assert.Equal(RenameListFieldCatalog.LoadErrorText, RenameListFieldCatalog.Resolve(item, pdfKey));
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
                var item = RenameItemFixtures.UnmarkedFromPath(path);
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
            var item = RenameItemFixtures.Unmarked("tiny-exif.jpeg");
            var makeKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271");

            Assert.False(item.ImagePropertiesLoadAttempted);
            RenameListMetadataLoader.TryEnsureLoaded(item, makeKey);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.Equal("Canon", RenameListFieldCatalog.Resolve(item, makeKey));
        }

        [Fact]
        public void Loads_pdf_info_from_fixture()
        {
            var item = RenameItemFixtures.Unmarked("tiny-info.pdf");
            var titleKey = RenameListFieldKey.Original(PdfRenameListFields.Group, PdfRenameListFields.Key.Title);

            Assert.False(item.PdfLoadAttempted);
            RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);

            Assert.True(item.PdfLoadAttempted);
            Assert.Equal("Sample Title", RenameListFieldCatalog.Resolve(item, titleKey));
        }

        [Fact]
        public void Loads_epub_info_from_fixture()
        {
            var item = RenameItemFixtures.Unmarked("tiny-info.epub");

            Assert.False(item.EpubLoadAttempted);
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Epub);

            Assert.True(item.EpubLoadAttempted);
            Assert.Null(item.EpubLoadError);
            Assert.NotNull(item.Original.Epub);
            Assert.Same(item.Original.Epub, item.Preview.Epub);
            Assert.Equal("Sample EPUB Title", item.Original.Epub.Title);
        }

        [Fact]
        public void Loads_office_info_from_fixture()
        {
            var item = RenameItemFixtures.Unmarked("tiny-info.docx");

            Assert.False(item.OfficeLoadAttempted);
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Office);

            Assert.True(item.OfficeLoadAttempted);
            Assert.Null(item.OfficeLoadError);
            Assert.NotNull(item.Original.Office);
            Assert.Same(item.Original.Office, item.Preview.Office);
            Assert.Equal("Sample Office Title", item.Original.Office.Title);
        }

        [Fact]
        public void Non_epub_soft_fails_epub_bucket()
        {
            var item = RenameItemFixtures.Unmarked("tiny.jpeg");

            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Epub);

            Assert.True(item.EpubLoadAttempted);
            Assert.NotNull(item.EpubLoadError);
            Assert.Null(item.Original.Epub);
        }

        [Fact]
        public void Non_docx_soft_fails_office_bucket()
        {
            var item = RenameItemFixtures.Unmarked("tiny.jpeg");

            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Office);

            Assert.True(item.OfficeLoadAttempted);
            Assert.NotNull(item.OfficeLoadError);
            Assert.Null(item.Original.Office);
        }

        [Fact]
        public void Clear_epub_cache_resets_flag_and_dto()
        {
            var item = RenameItemFixtures.Unmarked("tiny-info.epub");
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Epub);
            Assert.NotNull(item.Original.Epub);

            item.ClearEpubCache();

            Assert.False(item.EpubLoadAttempted);
            Assert.Null(item.EpubLoadError);
            Assert.Null(item.Original.Epub);
            Assert.Null(item.Preview.Epub);
        }

        [Fact]
        public void Clear_office_cache_resets_flag_and_dto()
        {
            var item = RenameItemFixtures.Unmarked("tiny-info.docx");
            RenameListMetadataLoader.TryEnsureLoaded(item, RenameListMetadataRequirement.Office);
            Assert.NotNull(item.Original.Office);

            item.ClearOfficeCache();

            Assert.False(item.OfficeLoadAttempted);
            Assert.Null(item.OfficeLoadError);
            Assert.Null(item.Original.Office);
            Assert.Null(item.Preview.Office);
        }

        [Fact]
        public void Loads_media_properties_from_disk_and_fills_tags()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "sample.mp3");
                File.Copy(FixturePaths.Require("l3-compl-cut.mp3"), path, overwrite: true);
                var item = RenameItemFixtures.UnmarkedFromPath(path);
                var layerKey = RenameListFieldKey.Original(Mp3RenameListFields.Group, "Layer");

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
            var item = RenameItemFixtures.Unmarked("tiny.jpeg");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            RenameListMetadataLoader.TryEnsureLoaded(item, titleKey);

            Assert.True(item.TagLibLoadAttempted);
            Assert.Null(item.TagLibMetadataLoadError);
            Assert.Equal(string.Empty, RenameListFieldCatalog.Resolve(item, titleKey));
        }

        [Fact]
        public void Clear_metadata_cache_clears_load_errors()
        {
            var item = RenameItemFixtures.UnmarkedFromPath(@"C:\DoesNotExist\Never\missing.mp3");
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
            var item = RenameItemFixtures.UnmarkedFromPath(@"C:\DoesNotExist\Never\missing.mp3");
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
            var item = RenameItemFixtures.Unmarked("tiny-exif.jpeg");
            var requirement =
                RenameListMetadataRequirement.TagLib
                | RenameListMetadataRequirement.ImageProperties
                | RenameListMetadataRequirement.Pdf
                | RenameListMetadataRequirement.Epub
                | RenameListMetadataRequirement.Office;

            Assert.True(RenameListMetadataLoader.AnyItemNeedsLoad([item], requirement));
            RenameListMetadataLoader.TryEnsureLoaded(item, requirement);

            Assert.True(item.TagLibLoadAttempted);
            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.True(item.PdfLoadAttempted);
            Assert.True(item.EpubLoadAttempted);
            Assert.True(item.OfficeLoadAttempted);
            Assert.False(RenameListMetadataLoader.AnyItemNeedsLoad([item], requirement));
        }
    }
}
