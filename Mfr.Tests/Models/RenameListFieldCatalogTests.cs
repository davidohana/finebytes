using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
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
            Assert.Equal(66, RenameListFieldCatalog.All.Count);
            Assert.Equal(9, RenameListFieldCatalog.GetFieldsForGroup(BasicRenameListField.Group).Count);
            Assert.Equal(6, RenameListFieldCatalog.GetFieldsForGroup(ExtendedRenameListField.Group).Count);
            Assert.Equal(32, RenameListFieldCatalog.GetFieldsForGroup(AudioTagRenameListField.Group).Count);
            Assert.Equal(7, RenameListFieldCatalog.GetFieldsForGroup(ImageRenameListField.Group).Count);
            Assert.Equal(12, RenameListFieldCatalog.GetFieldsForGroup(JpegRenameListField.Group).Count);
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

        [Theory]
        [InlineData(RenameListSortColumn.FileFolder, BasicRenameListFields.Key.ItemType)]
        [InlineData(RenameListSortColumn.ParentFolder, BasicRenameListFields.Key.Folder)]
        [InlineData(RenameListSortColumn.FullFileName, BasicRenameListFields.Key.FullName)]
        [InlineData(RenameListSortColumn.FullPath, BasicRenameListFields.Key.FullPath)]
        public void Sort_column_maps_to_original_field_key(RenameListSortColumn column, string propertyKey)
        {
            Assert.True(RenameListFieldCatalog.TryMapSortColumn(column, out var key));
            Assert.Equal(BasicRenameListField.Group, key.GroupId);
            Assert.Equal(propertyKey, key.PropertyKey);
            Assert.False(key.IsPreview);
        }

        [Fact]
        public void Sort_column_mapping_round_trips_for_engine_columns()
        {
            foreach (var column in Enum.GetValues<RenameListSortColumn>())
            {
                Assert.True(RenameListFieldCatalog.TryMapSortColumn(column, out var key));
                Assert.True(RenameListFieldCatalog.TryMapFieldKeyToSortColumn(key, out var roundTrip));
                Assert.Equal(column, roundTrip);
            }
        }

        [Fact]
        public void Preview_field_keys_do_not_map_to_sort_columns()
        {
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.False(RenameListFieldCatalog.TryMapFieldKeyToSortColumn(previewKey, out _));
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
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "vacation007",
                extension: ".jpg",
                directory: @"D:\Photos\2024"
            );

            _AssertField(item, BasicRenameListFields.Key.ItemType, "File");
            _AssertField(item, BasicRenameListFields.Key.Name, "vacation007");
            _AssertField(item, BasicRenameListFields.Key.Extension, "jpg");
            _AssertField(item, BasicRenameListFields.Key.FullName, "vacation007.jpg");
            _AssertField(item, BasicRenameListFields.Key.Folder, @"D:\Photos\2024");
            _AssertField(item, BasicRenameListFields.Key.FullPath, @"D:\Photos\2024\vacation007.jpg");
            _AssertField(item, BasicRenameListFields.Key.FileNameNumeric, "7");
            _AssertField(item, BasicRenameListFields.Key.FileNameLength, "15");
            _AssertField(item, BasicRenameListFields.Key.FullPathLength, "30");
        }

        [Fact]
        public void Resolve_folder_row_basic_fields()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Album",
                extension: "",
                directory: @"D:\Music",
                attributes: FileAttributes.Directory
            );

            _AssertField(item, BasicRenameListFields.Key.ItemType, "Folder");
            _AssertField(item, BasicRenameListFields.Key.Name, "Album");
            _AssertField(item, BasicRenameListFields.Key.Extension, "");
            _AssertField(item, BasicRenameListFields.Key.FullName, "Album");
            _AssertField(item, BasicRenameListFields.Key.Folder, @"D:\Music");
            _AssertField(item, BasicRenameListFields.Key.FullPath, @"D:\Music\Album");
            _AssertField(item, BasicRenameListFields.Key.FileNameNumeric, "0");
            _AssertField(item, BasicRenameListFields.Key.FileNameLength, "5");
            _AssertField(item, BasicRenameListFields.Key.FullPathLength, "14");
        }

        [Fact]
        public void Resolve_preview_field_uses_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            item.Preview.Prefix = "after";

            var originalKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.Equal("before.txt", RenameListFieldCatalog.Resolve(item, originalKey));
            Assert.Equal("after.txt", RenameListFieldCatalog.Resolve(item, previewKey));

            Assert.True(RenameListFieldCatalog.TryGetField(originalKey, out var field));
            Assert.Equal("before.txt", field.Resolve(item, isPreview: false));
            Assert.Equal("after.txt", field.Resolve(item, isPreview: true));
        }

        [Fact]
        public void GetField_returns_registered_field()
        {
            var expected = RenameListFieldCatalog.All.Single(field => field.PropertyKey == BasicRenameListFields.Key.ItemType);

            Assert.Same(expected, RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType));
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
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FileNameNumeric);

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

            _AssertField(item, ExtendedRenameListField.Group, "CreationDate", created.ToString("g"));
            _AssertField(item, ExtendedRenameListField.Group, "LastWriteDate", created.AddDays(1).ToString("g"));
            _AssertField(item, ExtendedRenameListField.Group, "LastAccessDate", created.AddDays(2).ToString("g"));
            _AssertField(item, ExtendedRenameListField.Group, "Size", "2048");
            _AssertField(item, ExtendedRenameListField.Group, "Attrs", "RA--");
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
                        Genre: "Rock",
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

            _AssertField(item, AudioTagRenameListField.Group, "Title", "Song");
            _AssertField(item, AudioTagRenameListField.Group, "Album", "Album");
            _AssertField(item, AudioTagRenameListField.Group, "Performers", "Alice; Bob");
            _AssertField(item, AudioTagRenameListField.Group, "FirstPerformer", "Alice");
            _AssertField(item, AudioTagRenameListField.Group, "FirstAlbumArtist", "Band");
            _AssertField(item, AudioTagRenameListField.Group, "Year", "2020");
            Assert.Contains(
                "Id3v2",
                RenameListFieldCatalog.Resolve(
                    item,
                    RenameListFieldKey.Original(AudioTagRenameListField.Group, "TagTypes")
                )
            );

            Assert.False(RenameListFieldCatalog.GetField(AudioTagRenameListField.Group, "Title").SupportsPreview);
            Assert.Equal(
                RenameListFieldMetadataLoad.EmbeddedAudioTags,
                RenameListFieldCatalog.GetMetadataLoad(RenameListFieldKey.Original(AudioTagRenameListField.Group, "Title"))
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
                        DateTaken = new DateTime(2024, 7, 4, 9, 30, 0, DateTimeKind.Unspecified),
                    };
                }
            );

            _AssertField(item, ImageRenameListField.Group, "Format", "JPEG");
            _AssertField(item, ImageRenameListField.Group, "Width", "1920");
            _AssertField(item, JpegRenameListField.Group, "ExifDirectory*271", "Canon");
            _AssertField(item, JpegRenameListField.Group, "ExifDirectory*40091", "Vacation");
            _AssertField(
                item,
                JpegRenameListField.Group,
                "ExifDirectory*36867",
                new DateTime(2024, 7, 4, 9, 30, 0, DateTimeKind.Unspecified).ToString("g")
            );

            Assert.False(RenameListFieldCatalog.GetField(ImageRenameListField.Group, "Width").SupportsPreview);
            Assert.Equal(
                RenameListFieldMetadataLoad.ImageProperties,
                RenameListFieldCatalog.GetCombinedMetadataLoad([
                    RenameListFieldKey.Original(ImageRenameListField.Group, "Width"),
                    RenameListFieldKey.Original(JpegRenameListField.Group, "ExifDirectory*271"),
                ])
            );
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
    }
}
