using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="RenameListFieldCatalog"/>.
    /// </summary>
    public sealed class RenameListFieldCatalogTests
    {
        [Fact]
        public void Basic_group_registers_nine_fields_in_catalog_order()
        {
            Assert.Equal(9, RenameListFieldCatalog.All.Count);
            Assert.Equal(
                [
                    BasicItemTypeField.Key,
                    BasicFolderField.Key,
                    BasicFullNameField.Key,
                    BasicFullPathField.Key,
                    BasicNameField.Key,
                    BasicExtensionField.Key,
                    BasicFileNameNumericField.Key,
                    BasicFileNameLengthField.Key,
                    BasicFullPathLengthField.Key,
                ],
                [.. RenameListFieldCatalog.All.Select(field => field.PropertyKey)]
            );
            Assert.Equal(
                RenameListFieldCatalog.GetFieldsForGroup(BasicRenameListField.Group),
                RenameListFieldCatalog.All
            );
            Assert.Empty(RenameListFieldCatalog.GetFieldsForGroup("Unknown"));
        }

        [Fact]
        public void Default_visible_columns_match_mfr7_rename_grid()
        {
            var keys = RenameListFieldCatalog.DefaultVisibleColumns;
            Assert.Equal(4, keys.Count);
            Assert.Equal(BasicItemTypeField.Key, keys[0].PropertyKey);
            Assert.False(keys[0].IsPreview);
            Assert.Equal(BasicFolderField.Key, keys[1].PropertyKey);
            Assert.False(keys[1].IsPreview);
            Assert.Equal(BasicFullNameField.Key, keys[2].PropertyKey);
            Assert.False(keys[2].IsPreview);
            Assert.Equal(BasicFullNameField.Key, keys[3].PropertyKey);
            Assert.True(keys[3].IsPreview);
        }

        [Theory]
        [InlineData(RenameListSortColumn.FileFolder, BasicItemTypeField.Key)]
        [InlineData(RenameListSortColumn.ParentFolder, BasicFolderField.Key)]
        [InlineData(RenameListSortColumn.FullFileName, BasicFullNameField.Key)]
        [InlineData(RenameListSortColumn.FullPath, BasicFullPathField.Key)]
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
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);

            Assert.False(RenameListFieldCatalog.TryMapFieldKeyToSortColumn(previewKey, out _));
        }

        [Theory]
        [InlineData(BasicNameField.Key, "File Name", 150, true, true)]
        [InlineData(BasicFolderField.Key, "Parent Folder", 240, true, true)]
        [InlineData(BasicFullNameField.Key, "Full File Name", 180, true, true)]
        [InlineData(BasicFullPathField.Key, "Full File Path", 180, true, true)]
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
        [InlineData(BasicItemTypeField.Key, "File/Folder", false)]
        [InlineData(BasicExtensionField.Key, "File Extension", true)]
        [InlineData(BasicFileNameNumericField.Key, "File Name Numeric Value", false)]
        [InlineData(BasicFileNameLengthField.Key, "File Name Length", true)]
        [InlineData(BasicFullPathLengthField.Key, "Full Path Name Length", true)]
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

            _AssertField(item, BasicItemTypeField.Key, "File");
            _AssertField(item, BasicNameField.Key, "vacation007");
            _AssertField(item, BasicExtensionField.Key, "jpg");
            _AssertField(item, BasicFullNameField.Key, "vacation007.jpg");
            _AssertField(item, BasicFolderField.Key, @"D:\Photos\2024");
            _AssertField(item, BasicFullPathField.Key, @"D:\Photos\2024\vacation007.jpg");
            _AssertField(item, BasicFileNameNumericField.Key, "7");
            _AssertField(item, BasicFileNameLengthField.Key, "15");
            _AssertField(item, BasicFullPathLengthField.Key, "30");
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

            _AssertField(item, BasicItemTypeField.Key, "Folder");
            _AssertField(item, BasicNameField.Key, "Album");
            _AssertField(item, BasicExtensionField.Key, "");
            _AssertField(item, BasicFullNameField.Key, "Album");
            _AssertField(item, BasicFolderField.Key, @"D:\Music");
            _AssertField(item, BasicFullPathField.Key, @"D:\Music\Album");
            _AssertField(item, BasicFileNameNumericField.Key, "0");
            _AssertField(item, BasicFileNameLengthField.Key, "5");
            _AssertField(item, BasicFullPathLengthField.Key, "14");
        }

        [Fact]
        public void Resolve_preview_field_uses_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            item.Preview.Prefix = "after";

            var originalKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicFullNameField.Key);
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);

            Assert.Equal("before.txt", RenameListFieldCatalog.Resolve(item, originalKey));
            Assert.Equal("after.txt", RenameListFieldCatalog.Resolve(item, previewKey));

            Assert.True(RenameListFieldCatalog.TryGetField(originalKey, out var field));
            Assert.Equal("before.txt", field.Resolve(item, isPreview: false));
            Assert.Equal("after.txt", field.Resolve(item, isPreview: true));
        }

        [Fact]
        public void GetField_returns_registered_field()
        {
            var expected = RenameListFieldCatalog.All.Single(field => field.PropertyKey == BasicItemTypeField.Key);

            Assert.Same(expected, RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicItemTypeField.Key));
            Assert.Same(
                expected,
                RenameListFieldCatalog.GetField(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicItemTypeField.Key)
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
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicFileNameNumericField.Key);

            Assert.Equal(expected, RenameListFieldCatalog.Resolve(item, key));
        }

        private static void _AssertField(RenameItem item, string propertyKey, string expected)
        {
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, propertyKey);
            Assert.Equal(expected, RenameListFieldCatalog.Resolve(item, key));
        }
    }
}
