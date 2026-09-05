using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;
using Mfr.Models.Tags;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="RenameListFieldCatalog"/>.
    /// </summary>
    public sealed class RenameListFieldCatalogTests
    {
        [Fact]
        public void Catalog_registers_all_phase7a_original_field_groups()
        {
            Assert.Equal(97, RenameListFieldCatalog.All.Count);
            Assert.Equal(9, RenameListFieldCatalog.GetFieldsForGroup(BasicRenameListField.Group).Count);
            Assert.Equal(6, RenameListFieldCatalog.GetFieldsForGroup(ExtendedRenameListFields.Group).Count);
            Assert.Equal(32, RenameListFieldCatalog.GetFieldsForGroup(AudioTagRenameListFields.Group).Count);
            Assert.Equal(15, RenameListFieldCatalog.GetFieldsForGroup(MediaRenameListFields.Group).Count);
            Assert.Equal(11, RenameListFieldCatalog.GetFieldsForGroup(MpegRenameListFields.Group).Count);
            Assert.Equal(7, RenameListFieldCatalog.GetFieldsForGroup(ImageRenameListFields.Group).Count);
            Assert.Equal(17, RenameListFieldCatalog.GetFieldsForGroup(JpegRenameListFields.Group).Count);
        }

        [Fact]
        public void Catalog_group_order_matches_shuttle_dropdown()
        {
            Assert.Equal(
                [
                    BasicRenameListField.GroupLabel,
                    ExtendedRenameListFields.GroupLabel,
                    AudioTagRenameListFields.GroupLabel,
                    MediaRenameListFields.GroupLabel,
                    MpegRenameListFields.GroupLabel,
                    ImageRenameListFields.GroupLabel,
                    JpegRenameListFields.GroupLabel,
                ],
                RenameListFieldCatalog.All.Select(field => field.GroupDisplayName).Distinct()
            );
        }

        [Fact]
        public void Basic_group_registers_nine_fields_in_catalog_order()
        {
            Assert.Equal(
                [
                    BasicRenameListFields.Key.ItemType,
                    BasicRenameListFields.Key.Folder,
                    BasicRenameListFields.Key.FullName,
                    BasicRenameListFields.Key.FullPath,
                    BasicRenameListFields.Key.Name,
                    BasicRenameListFields.Key.Extension,
                    BasicRenameListFields.Key.FileNameNumeric,
                    BasicRenameListFields.Key.FileNameLength,
                    BasicRenameListFields.Key.FullPathLength,
                ],
                [
                    .. RenameListFieldCatalog
                        .GetFieldsForGroup(BasicRenameListField.Group)
                        .Select(field => field.PropertyKey),
                ]
            );
            Assert.Equal(
                RenameListFieldCatalog.GetFieldsForGroup(BasicRenameListField.Group),
                [.. RenameListFieldCatalog.All.Where(field => field.GroupId == BasicRenameListField.Group)]
            );
            Assert.Empty(RenameListFieldCatalog.GetFieldsForGroup("Unknown"));
        }

        [Fact]
        public void Default_visible_columns_match_mfr7_rename_grid()
        {
            var keys = RenameListFieldCatalog.DefaultVisibleColumns;
            Assert.Equal(4, keys.Count);
            Assert.Equal(BasicRenameListFields.Key.ItemType, keys[0].PropertyKey);
            Assert.False(keys[0].IsPreview);
            Assert.Equal(BasicRenameListFields.Key.Folder, keys[1].PropertyKey);
            Assert.False(keys[1].IsPreview);
            Assert.Equal(BasicRenameListFields.Key.FullName, keys[2].PropertyKey);
            Assert.False(keys[2].IsPreview);
            Assert.Equal(BasicRenameListFields.Key.FullName, keys[3].PropertyKey);
            Assert.True(keys[3].IsPreview);
        }

        [Fact]
        public void Every_original_field_is_sortable_and_preview_keys_are_not()
        {
            foreach (var field in RenameListFieldCatalog.All)
            {
                Assert.True(RenameListFieldCatalog.IsSortableKey(field.OriginalKey), field.PropertyKey);
                Assert.False(RenameListFieldCatalog.IsSortableKey(field.PreviewKey), field.PropertyKey);
            }
        }

        [Theory]
        [InlineData(BasicRenameListFields.Key.Name, "File Name", 150, true, true)]
        [InlineData(BasicRenameListFields.Key.Folder, "Parent Folder", 240, true, true)]
        [InlineData(BasicRenameListFields.Key.FullName, "Full File Name", 180, true, true)]
        [InlineData(BasicRenameListFields.Key.FullPath, "Full File Path", 180, true, true)]
        public void Field_definitions_with_width_overrides_carry_mfr7_labels_and_flags(
            string propertyKey,
            string displayName,
            int defaultWidth,
            bool isSortable,
            bool supportsPreview
        )
        {
            Assert.True(RenameListFieldCatalog.TryGetField(BasicRenameListField.Group, propertyKey, out var field));
            Assert.Equal(displayName, field.DisplayName);
            Assert.Equal(BasicRenameListField.GroupLabel, field.GroupDisplayName);
            Assert.Equal(defaultWidth, field.DefaultWidth);
            Assert.Equal(isSortable, field.IsSortable);
            Assert.Equal(supportsPreview, field.SupportsPreview);
            Assert.False(field.OriginalKey.IsPreview);
            Assert.True(field.PreviewKey.IsPreview);
        }

        [Theory]
        [InlineData(BasicRenameListFields.Key.ItemType, "File/Folder", false)]
        [InlineData(BasicRenameListFields.Key.Extension, "File Extension", true)]
        [InlineData(BasicRenameListFields.Key.FileNameNumeric, "File Name Numeric Value", false)]
        [InlineData(BasicRenameListFields.Key.FileNameLength, "File Name Length", true)]
        [InlineData(BasicRenameListFields.Key.FullPathLength, "Full Path Name Length", true)]
        public void Field_definitions_without_width_overrides_use_header_fit_default(
            string propertyKey,
            string displayName,
            bool supportsPreview
        )
        {
            Assert.True(RenameListFieldCatalog.TryGetField(BasicRenameListField.Group, propertyKey, out var field));
            Assert.Equal(displayName, field.DisplayName);
            Assert.Null(field.DefaultWidth);
            Assert.Equal(supportsPreview, field.SupportsPreview);
        }

        [Fact]
        public void Resolve_file_row_basic_fields()
        {
            var directory = TestPaths.Absolute("Photos", "2024");
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "vacation007",
                extension: ".jpg",
                directory: directory
            );
            var fullPath = Path.Combine(directory, "vacation007.jpg");

            _AssertField(item, BasicRenameListFields.Key.ItemType, "File");
            _AssertField(item, BasicRenameListFields.Key.Name, "vacation007");
            _AssertField(item, BasicRenameListFields.Key.Extension, "jpg");
            _AssertField(item, BasicRenameListFields.Key.FullName, "vacation007.jpg");
            _AssertField(item, BasicRenameListFields.Key.Folder, directory);
            _AssertField(item, BasicRenameListFields.Key.FullPath, fullPath);
            _AssertField(item, BasicRenameListFields.Key.FileNameNumeric, "7");
            _AssertField(item, BasicRenameListFields.Key.FileNameLength, "15");
            _AssertField(item, BasicRenameListFields.Key.FullPathLength, fullPath.Length.ToString());
        }

        [Fact]
        public void Resolve_folder_row_basic_fields()
        {
            var directory = TestPaths.Absolute("Music");
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Album",
                extension: "",
                directory: directory,
                attributes: FileAttributes.Directory
            );
            var fullPath = Path.Combine(directory, "Album");

            _AssertField(item, BasicRenameListFields.Key.ItemType, "Folder");
            _AssertField(item, BasicRenameListFields.Key.Name, "Album");
            _AssertField(item, BasicRenameListFields.Key.Extension, "");
            _AssertField(item, BasicRenameListFields.Key.FullName, "Album");
            _AssertField(item, BasicRenameListFields.Key.Folder, directory);
            _AssertField(item, BasicRenameListFields.Key.FullPath, fullPath);
            _AssertField(item, BasicRenameListFields.Key.FileNameNumeric, "0");
            _AssertField(item, BasicRenameListFields.Key.FileNameLength, "5");
            _AssertField(item, BasicRenameListFields.Key.FullPathLength, fullPath.Length.ToString());
        }

        [Fact]
        public void Resolve_preview_field_uses_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            item.Preview.Prefix = "after";

            var originalKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.Equal("before.txt", RenameListFieldCatalog.Resolve(item, originalKey));
            Assert.Equal("after.txt", RenameListFieldCatalog.Resolve(item, previewKey));

            Assert.True(RenameListFieldCatalog.TryGetField(originalKey, out var field));
            Assert.Equal("before.txt", field.Resolve(item, isPreview: false));
            Assert.Equal("after.txt", field.Resolve(item, isPreview: true));
        }

        [Fact]
        public void IsPreviewChanged_true_only_for_changed_preview_keys()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            var originalKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.False(RenameListFieldCatalog.IsPreviewChanged(item, originalKey));
            Assert.False(RenameListFieldCatalog.IsPreviewChanged(item, previewKey));

            item.Preview.Prefix = "after";

            Assert.False(RenameListFieldCatalog.IsPreviewChanged(item, originalKey));
            Assert.True(RenameListFieldCatalog.IsPreviewChanged(item, previewKey));
        }

        [Fact]
        public void GetField_returns_registered_field()
        {
            var expected = RenameListFieldCatalog.All.Single(field =>
                field.PropertyKey == BasicRenameListFields.Key.ItemType
            );

            Assert.Same(
                expected,
                RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
            );
            Assert.Same(
                expected,
                RenameListFieldCatalog.GetField(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                )
            );
        }

        [Fact]
        public void GetField_unknown_field_throws()
        {
            var key = RenameListFieldKey.Original("Unknown", "Missing");

            Assert.Throws<ArgumentException>(() => RenameListFieldCatalog.GetField("Unknown", "Missing"));
            Assert.Throws<ArgumentException>(() => RenameListFieldCatalog.GetField(key));
        }

        [Fact]
        public void Resolve_unknown_field_throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var key = RenameListFieldKey.Original("Unknown", "Missing");

            Assert.Throws<ArgumentException>(() => RenameListFieldCatalog.Resolve(item, key));
        }

        [Theory]
        [InlineData("track01", ".mp3", "1")]
        [InlineData("no-digits", ".txt", "0")]
        [InlineData("img00042", ".png", "42")]
        public void Resolve_file_name_numeric_matches_mfr7_first_digit_run(
            string prefix,
            string extension,
            string expected
        )
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix, extension: extension);
            var key = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FileNameNumeric
            );

            Assert.Equal(expected, RenameListFieldCatalog.Resolve(item, key));
        }

        [Fact]
        public void Extended_fields_resolve_from_scan_metadata()
        {
            var created = new DateTime(2023, 1, 2, 15, 4, 5, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: created,
                lastWriteTime: created.AddDays(1),
                lastAccessTime: created.AddDays(2),
                fileSize: 2048,
                attributes: FileAttributes.ReadOnly | FileAttributes.Archive
            );

            _AssertField(item, ExtendedRenameListFields.Group, "CreationDate", created.ToString("g"));
            _AssertField(item, ExtendedRenameListFields.Group, "LastWriteDate", created.AddDays(1).ToString("g"));
            _AssertField(item, ExtendedRenameListFields.Group, "LastAccessDate", created.AddDays(2).ToString("g"));
            _AssertField(item, ExtendedRenameListFields.Group, "Size", "2048");
            _AssertField(item, ExtendedRenameListFields.Group, "Attrs", "RA--");
        }

        [Fact]
        public void Audio_tag_fields_resolve_semantic_overlay_without_preview()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: meta =>
            {
                meta.AudioTagOverlay.ContainerFormat = AudioContainerFormat.Mpeg;
                meta.AudioTagOverlay.ClearAllBlocks();
                meta.AudioTagOverlay.MergeSemantic(
                    new SemanticAudioTag(
                        Title: "Song",
                        Album: "Album",
                        Performers: "Alice;Bob",
                        AlbumArtists: "Band;Guest",
                        Composers: "Composer A;Composer B",
                        Genre: "Rock;Jazz",
                        Comment: null,
                        Lyrics: null,
                        Copyright: null,
                        Grouping: null,
                        Year: 2020,
                        Track: 3,
                        TrackCount: 10,
                        Disc: 1,
                        DiscCount: 2,
                        BeatsPerMinute: null,
                        Conductor: null,
                        MusicBrainzArtistId: null,
                        MusicBrainzReleaseId: null,
                        MusicBrainzReleaseArtistId: null,
                        MusicBrainzTrackId: null,
                        MusicBrainzDiscId: null,
                        MusicBrainzReleaseStatus: null,
                        MusicBrainzReleaseType: null,
                        MusicBrainzReleaseCountry: null,
                        MusicIpId: null,
                        AmazonId: null
                    )
                );
            });

            _AssertField(item, AudioTagRenameListFields.Group, "Title", "Song");
            _AssertField(item, AudioTagRenameListFields.Group, "Album", "Album");
            _AssertField(item, AudioTagRenameListFields.Group, "Performers", "Alice; Bob");
            _AssertField(item, AudioTagRenameListFields.Group, "FirstPerformer", "Alice");
            _AssertField(item, AudioTagRenameListFields.Group, "FirstAlbumArtist", "Band");
            _AssertField(item, AudioTagRenameListFields.Group, "FirstComposer", "Composer A");
            _AssertField(item, AudioTagRenameListFields.Group, "FirstGenre", "Rock");
            _AssertField(item, AudioTagRenameListFields.Group, "Genres", "Rock;Jazz");
            _AssertField(item, AudioTagRenameListFields.Group, "Year", "2020");
            Assert.Contains(
                "Id3v2",
                RenameListFieldCatalog.Resolve(
                    item,
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, "TagTypes")
                )
            );

            Assert.False(RenameListFieldCatalog.GetField(AudioTagRenameListFields.Group, "Title").SupportsPreview);
            Assert.Equal(
                RenameListMetadataRequirement.TagLib,
                RenameListFieldCatalog.GetMetadataRequirement(
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title")
                )
            );
        }

        [Fact]
        public void Image_and_jpeg_fields_resolve_cached_metadata()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                extension: ".jpg",
                configureOriginal: meta =>
                {
                    meta.Image = new ImageProperties
                    {
                        Format = "JPEG",
                        Width = 1920,
                        Height = 1080,
                        BitDepth = 24,
                        HorizontalResolutionDpi = 72,
                        VerticalResolutionDpi = 72,
                        FrameCount = 1,
                    };
                    meta.Exif = new ExifData
                    {
                        Make = "Canon",
                        Model = "EOS",
                        Title = "Vacation",
                        Artist = "Pat",
                        DateTaken = new DateTime(2024, 7, 4, 9, 30, 0, DateTimeKind.Unspecified),
                        TagToDescription = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["ExifSub/37393"] = "17",
                        },
                    };
                }
            );

            _AssertField(item, ImageRenameListFields.Group, "Format", "JPEG");
            _AssertField(item, ImageRenameListFields.Group, "Width", "1920");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*271", "Canon");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*40091", "Vacation");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*315", "Pat");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*37393", "17");
            _AssertField(
                item,
                JpegRenameListFields.Group,
                "ExifDirectory*36867",
                new DateTime(2024, 7, 4, 9, 30, 0, DateTimeKind.Unspecified).ToString("g")
            );

            Assert.False(RenameListFieldCatalog.GetField(ImageRenameListFields.Group, "Width").SupportsPreview);
            Assert.Equal(
                RenameListMetadataRequirement.ImageProperties,
                RenameListFieldCatalog.GetMetadataRequirement(
                    RenameListFieldKey.Original(ImageRenameListFields.Group, "Width")
                )
            );
            Assert.Equal(
                RenameListMetadataRequirement.ImageProperties,
                RenameListFieldCatalog.GetMetadataRequirement(
                    RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*271")
                )
            );
            Assert.Equal(
                RenameListMetadataRequirement.None,
                RenameListFieldCatalog.GetMetadataRequirement(RenameListFieldKey.Original("Unknown", "Missing"))
            );
        }

        [Fact]
        public void Media_mpeg_and_jpeg_camera_fields_resolve_cached_metadata()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                extension: ".mp3",
                configureOriginal: meta =>
                {
                    meta.Media = new MediaProperties
                    {
                        MimeType = "taglib/mp3",
                        PossiblyCorrupt = false,
                        Duration = TimeSpan.FromSeconds(225),
                        MediaTypes = "Audio",
                        Description = "MPEG Version 1 Audio, Layer 3",
                        AudioBitrate = 128,
                        AudioSampleRate = 44100,
                        BitsPerSample = 16,
                        AudioChannels = 2,
                        PhotoWidth = 1920,
                        PhotoHeight = 1080,
                        PhotoQuality = 85,
                        Mpeg = new MpegAudioProperties
                        {
                            Bitrate = 128,
                            IsCopyrighted = true,
                            Duration = TimeSpan.FromSeconds(225),
                            IsVbr = false,
                            SampleRate = 44100,
                            Layer = 3,
                            MpegVersion = "1",
                            ChannelMode = "JointStereo",
                            IsOriginal = false,
                            IsProtected = true,
                        },
                    };
                    meta.Exif = new ExifData
                    {
                        Exposure = "1/250 sec",
                        FNumber = "f/5.6",
                        Iso = "400",
                        FocalLength = "50 mm",
                        FocalLength35mm = "75 mm",
                    };
                }
            );

            _AssertField(item, MediaRenameListFields.Group, "MimeType", "taglib/mp3");
            _AssertField(item, MediaRenameListFields.Group, "PossiblyCorrupt", "No");
            _AssertField(item, MediaRenameListFields.Group, "Duration", "0:03:45");
            _AssertField(item, MediaRenameListFields.Group, "DurationSeconds", "225");
            _AssertField(item, MediaRenameListFields.Group, "MediaTypes", "Audio");
            _AssertField(item, MediaRenameListFields.Group, "AudioBitrate", "128");
            _AssertField(item, MediaRenameListFields.Group, "PhotoWidth", "1920");
            _AssertField(item, MpegRenameListFields.Group, "Bitrate", "128");
            _AssertField(item, MpegRenameListFields.Group, "Copyright", "Yes");
            _AssertField(item, MpegRenameListFields.Group, "Duration", "0:03:45");
            _AssertField(item, MpegRenameListFields.Group, "VBR", "CBR");
            _AssertField(item, MpegRenameListFields.Group, "Layer", "III");
            _AssertField(item, MpegRenameListFields.Group, "Mode", "JointStereo");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*33434", "1/250 sec");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*33437", "f/5.6");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*34855", "400");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*37386", "50 mm");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*41989", "75 mm");

            Assert.Equal(
                RenameListMetadataRequirement.TagLib,
                RenameListFieldCatalog.GetMetadataRequirement(
                    RenameListFieldKey.Original(MediaRenameListFields.Group, "MimeType")
                )
            );
            Assert.Equal(
                RenameListMetadataRequirement.TagLib,
                RenameListFieldCatalog.GetMetadataRequirement(
                    RenameListFieldKey.Original(MpegRenameListFields.Group, "Bitrate")
                )
            );
        }

        [Fact]
        public void Phase7a_original_groups_are_sortable_without_preview()
        {
            string[] originalOnlyGroups =
            [
                ExtendedRenameListFields.Group,
                AudioTagRenameListFields.Group,
                MediaRenameListFields.Group,
                MpegRenameListFields.Group,
                ImageRenameListFields.Group,
                JpegRenameListFields.Group,
            ];

            foreach (
                var field in RenameListFieldCatalog.All.Where(catalogField =>
                    originalOnlyGroups.Contains(catalogField.GroupId)
                )
            )
            {
                Assert.False(field.SupportsPreview, field.PropertyKey);
                Assert.True(field.IsSortable, field.PropertyKey);
                Assert.True(RenameListFieldCatalog.IsSortableKey(field.OriginalKey), field.PropertyKey);
            }
        }

        [Fact]
        public void Original_only_fields_resolve_empty_when_metadata_is_absent()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: DateTime.MinValue,
                lastWriteTime: DateTime.MinValue,
                lastAccessTime: DateTime.MinValue
            );
            item.Original.AudioTagOverlay.ClearAllBlocks();
            item.Original.Image = null;
            item.Original.Exif = null;
            item.Original.Media = null;

            _AssertField(item, ExtendedRenameListFields.Group, "CreationDate", "");
            _AssertField(item, AudioTagRenameListFields.Group, "Title", "");
            _AssertField(item, AudioTagRenameListFields.Group, "TagTypes", "");
            _AssertField(item, MediaRenameListFields.Group, "MimeType", "");
            _AssertField(item, MediaRenameListFields.Group, "Duration", "");
            _AssertField(item, MpegRenameListFields.Group, "Bitrate", "");
            _AssertField(item, ImageRenameListFields.Group, "Width", "");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*271", "");
            _AssertField(item, JpegRenameListFields.Group, "ExifDirectory*33434", "");
        }

        [Fact]
        public void CompareForSort_orders_field_load_errors_after_non_error_values()
        {
            var errored = FilterTestHelpers.CreateRenameItem(prefix: "bad", extension: ".mp3");
            errored.SetTagLibMetadataLoadError(new IOException("missing file"));
            var alphaTitle = _ItemWithSemantic(SemanticAudioField.Title, "Alpha");
            var zebraTitle = _ItemWithSemantic(SemanticAudioField.Title, "Zebra");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            Assert.Equal(RenameListFieldCatalog.LoadErrorText, RenameListFieldCatalog.Resolve(errored, titleKey));
            Assert.True(RenameListFieldCatalog.CompareForSort(errored, titleKey, alphaTitle) > 0);
            Assert.True(RenameListFieldCatalog.CompareForSort(alphaTitle, titleKey, errored) < 0);
            Assert.True(RenameListFieldCatalog.CompareForSort(errored, titleKey, zebraTitle) > 0);
            Assert.True(RenameListFieldCatalog.CompareForSort(zebraTitle, titleKey, errored) < 0);

            var otherErrored = FilterTestHelpers.CreateRenameItem(prefix: "also-bad", extension: ".mp3");
            otherErrored.SetTagLibMetadataLoadError(new IOException("also missing"));
            Assert.Equal(0, RenameListFieldCatalog.CompareForSort(errored, titleKey, otherErrored));
        }

        [Fact]
        public void CompareForSort_does_not_treat_literal_Error_tag_value_as_load_failure()
        {
            var literalErrorTitle = _ItemWithSemantic(SemanticAudioField.Title, "Error");
            var alphaTitle = _ItemWithSemantic(SemanticAudioField.Title, "Alpha");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            Assert.Equal("Error", RenameListFieldCatalog.Resolve(literalErrorTitle, titleKey));
            Assert.False(RenameListMetadataLoadErrors.HasLoadError(literalErrorTitle, titleKey));
            Assert.True(RenameListFieldCatalog.CompareForSort(alphaTitle, titleKey, literalErrorTitle) < 0);
        }

        [Fact]
        public void CompareForSort_orders_audio_title_string()
        {
            var betaTitle = _ItemWithSemantic(SemanticAudioField.Title, "Beta");
            var alphaTitle = _ItemWithSemantic(SemanticAudioField.Title, "Alpha");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            Assert.True(RenameListFieldCatalog.CompareForSort(betaTitle, titleKey, alphaTitle) > 0);
        }

        [Fact]
        public void CompareForSort_orders_audio_track_and_image_width_numeric()
        {
            var trackTen = _ItemWithSemantic(SemanticAudioField.Track, "10");
            var trackTwo = _ItemWithSemantic(SemanticAudioField.Track, "2");
            var trackKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Track");
            Assert.True(RenameListFieldCatalog.CompareForSort(trackTwo, trackKey, trackTen) < 0);

            var widthTen = FilterTestHelpers.CreateRenameItem(
                prefix: "wide",
                extension: ".jpg",
                configureOriginal: meta => meta.Image = new ImageProperties { Width = 10 }
            );
            var widthTwo = FilterTestHelpers.CreateRenameItem(
                prefix: "narrow",
                extension: ".jpg",
                configureOriginal: meta => meta.Image = new ImageProperties { Width = 2 }
            );
            var widthKey = RenameListFieldKey.Original(ImageRenameListFields.Group, "Width");
            Assert.True(RenameListFieldCatalog.CompareForSort(widthTwo, widthKey, widthTen) < 0);
        }

        [Fact]
        public void CompareForSort_orders_media_duration_and_file_name_numeric()
        {
            var tenHours = FilterTestHelpers.CreateRenameItem(
                prefix: "long",
                extension: ".mp3",
                configureOriginal: meta => meta.Media = new MediaProperties { Duration = TimeSpan.FromHours(10) }
            );
            var oneHour = FilterTestHelpers.CreateRenameItem(
                prefix: "short",
                extension: ".mp3",
                configureOriginal: meta => meta.Media = new MediaProperties { Duration = TimeSpan.FromHours(1) }
            );
            var durationKey = RenameListFieldKey.Original(MediaRenameListFields.Group, "Duration");
            Assert.True(RenameListFieldCatalog.CompareForSort(oneHour, durationKey, tenHours) < 0);

            var fileTen = FilterTestHelpers.CreateRenameItem(prefix: "file10", extension: ".txt");
            var fileTwo = FilterTestHelpers.CreateRenameItem(prefix: "file2", extension: ".txt");
            var numericKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FileNameNumeric
            );
            Assert.True(RenameListFieldCatalog.CompareForSort(fileTwo, numericKey, fileTen) < 0);
        }

        [Fact]
        public void CompareForSort_orders_jpeg_image_number_numeric()
        {
            var lowImageItem = FilterTestHelpers.CreateRenameItem(
                prefix: "low",
                extension: ".jpg",
                configureOriginal: meta =>
                {
                    meta.Exif = new ExifData
                    {
                        TagToDescription = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["ExifSub/37393"] = "10",
                        },
                    };
                }
            );
            var highImageItem = FilterTestHelpers.CreateRenameItem(
                prefix: "high",
                extension: ".jpg",
                configureOriginal: meta =>
                {
                    meta.Exif = new ExifData
                    {
                        TagToDescription = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["ExifSub/37393"] = "2",
                        },
                    };
                }
            );

            var imageNumberKey = RenameListFieldKey.Original(JpegRenameListFields.Group, "ExifDirectory*37393");
            Assert.True(RenameListFieldCatalog.CompareForSort(highImageItem, imageNumberKey, lowImageItem) < 0);
        }

        [Fact]
        public void CompareForSort_throws_for_preview_key()
        {
            var left = FilterTestHelpers.CreateRenameItem(prefix: "alpha");
            var right = FilterTestHelpers.CreateRenameItem(prefix: "beta");
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.Throws<ArgumentException>(() => RenameListFieldCatalog.CompareForSort(left, previewKey, right));
        }

        [Fact]
        public void File_count_field_matches_token_rules()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var folderPath = Path.Combine(tempDir, "Album");
            Directory.CreateDirectory(folderPath);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");
                Directory.CreateDirectory(Path.Combine(tempDir, "sub"));
                File.WriteAllText(Path.Combine(folderPath, "track1.mp3"), "");

                var fileItem = FilterTestHelpers.CreateRenameItem(directory: tempDir);
                _AssertField(fileItem, ExtendedRenameListFields.Group, "FileCount", "2");

                var folderItem = FilterTestHelpers.CreateRenameItem(
                    prefix: "Album",
                    extension: "",
                    directory: tempDir,
                    attributes: FileAttributes.Directory
                );
                _AssertField(folderItem, ExtendedRenameListFields.Group, "FileCount", "1");
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }

            var missing = FilterTestHelpers.CreateRenameItem(directory: @"C:\DoesNotExist\Never");
            _AssertField(missing, ExtendedRenameListFields.Group, "FileCount", "");
        }

        private static void _AssertField(RenameItem item, string groupId, string propertyKey, string expected)
        {
            var key = RenameListFieldKey.Original(groupId, propertyKey);
            Assert.Equal(expected, RenameListFieldCatalog.Resolve(item, key));
        }

        private static void _AssertField(RenameItem item, string propertyKey, string expected)
        {
            _AssertField(item, BasicRenameListField.Group, propertyKey, expected);
        }

        private static RenameItem _ItemWithSemantic(SemanticAudioField field, string value)
        {
            return FilterTestHelpers.CreateRenameItem(
                prefix: "track",
                extension: ".mp3",
                configureOriginal: meta =>
                {
                    meta.AudioTagOverlay.ContainerFormat = AudioContainerFormat.Mpeg;
                    meta.AudioTagOverlay.ClearAllBlocks();
                    SemanticFields.SetSemanticField(meta.AudioTagOverlay, field, value);
                }
            );
        }
    }
}
