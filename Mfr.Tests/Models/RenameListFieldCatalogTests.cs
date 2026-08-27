using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="RenameListFieldCatalog"/> and <see cref="RenameListFieldValueResolver"/>.
    /// </summary>
    public sealed class RenameListFieldCatalogTests
    {
        [Fact]
        public void Basic_group_registers_nine_fields_in_catalog_order()
        {
            Assert.Equal(9, RenameListFieldCatalog.All.Count);
            Assert.Equal(
                RenameListBasicPropertyKeys.All,
                [.. RenameListFieldCatalog.All.Select(def => def.PropertyKey)]
            );
            Assert.Equal(
                RenameListFieldCatalog.BasicFields,
                RenameListFieldCatalog.GetDefinitionsForGroup(RenameListFieldCatalog.BasicGroupId)
            );
            Assert.Empty(RenameListFieldCatalog.GetDefinitionsForGroup("Unknown"));
        }

        [Fact]
        public void Default_visible_columns_match_mfr7_rename_grid()
        {
            var keys = RenameListFieldCatalog.DefaultVisibleColumns;
            Assert.Equal(4, keys.Count);
            Assert.Equal(RenameListBasicPropertyKeys.ItemType, keys[0].PropertyKey);
            Assert.False(keys[0].IsPreview);
            Assert.Equal(RenameListBasicPropertyKeys.Folder, keys[1].PropertyKey);
            Assert.False(keys[1].IsPreview);
            Assert.Equal(RenameListBasicPropertyKeys.FullName, keys[2].PropertyKey);
            Assert.False(keys[2].IsPreview);
            Assert.Equal(RenameListBasicPropertyKeys.FullName, keys[3].PropertyKey);
            Assert.True(keys[3].IsPreview);
        }

        [Theory]
        [InlineData(RenameListSortColumn.FileFolder, RenameListBasicPropertyKeys.ItemType)]
        [InlineData(RenameListSortColumn.ParentFolder, RenameListBasicPropertyKeys.Folder)]
        [InlineData(RenameListSortColumn.FullFileName, RenameListBasicPropertyKeys.FullName)]
        [InlineData(RenameListSortColumn.FullPath, RenameListBasicPropertyKeys.FullPath)]
        public void Sort_column_maps_to_original_field_key(RenameListSortColumn column, string propertyKey)
        {
            Assert.True(RenameListFieldCatalog.TryMapSortColumn(column, out var key));
            Assert.Equal(RenameListFieldCatalog.BasicGroupId, key.GroupId);
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
            var previewKey = RenameListFieldKey.Preview(
                RenameListFieldCatalog.BasicGroupId,
                RenameListBasicPropertyKeys.FullName
            );

            Assert.False(RenameListFieldCatalog.TryMapFieldKeyToSortColumn(previewKey, out _));
        }

        [Theory]
        [InlineData(RenameListBasicPropertyKeys.Name, "File Name", 150, true, true)]
        [InlineData(RenameListBasicPropertyKeys.ItemType, "File/Folder", 50, true, false)]
        [InlineData(RenameListBasicPropertyKeys.FileNameNumeric, "File Name Numeric Value", 50, true, false)]
        public void Field_definitions_carry_mfr7_labels_and_flags(
            string propertyKey,
            string displayName,
            int defaultWidth,
            bool isSortable,
            bool supportsPreview
        )
        {
            Assert.True(
                RenameListFieldCatalog.TryGetDefinition(
                    RenameListFieldCatalog.BasicGroupId,
                    propertyKey,
                    out var definition
                )
            );
            Assert.NotNull(definition);
            Assert.Equal(displayName, definition.DisplayName);
            Assert.Equal(RenameListFieldCatalog.BasicGroupDisplayName, definition.GroupDisplayName);
            Assert.Equal(defaultWidth, definition.DefaultWidth);
            Assert.Equal(isSortable, definition.IsSortable);
            Assert.Equal(supportsPreview, definition.SupportsPreview);
            Assert.Equal(definition.OriginalKey, definition.OriginalKey with { IsPreview = false });
            Assert.True(definition.PreviewKey.IsPreview);
        }

        [Fact]
        public void Resolve_file_row_basic_fields()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "vacation007",
                extension: ".jpg",
                directory: @"D:\Photos\2024"
            );

            _AssertField(item, RenameListBasicPropertyKeys.ItemType, "File");
            _AssertField(item, RenameListBasicPropertyKeys.Name, "vacation007");
            _AssertField(item, RenameListBasicPropertyKeys.Extension, "jpg");
            _AssertField(item, RenameListBasicPropertyKeys.FullName, "vacation007.jpg");
            _AssertField(item, RenameListBasicPropertyKeys.Folder, @"D:\Photos\2024");
            _AssertField(item, RenameListBasicPropertyKeys.FullPath, @"D:\Photos\2024\vacation007.jpg");
            _AssertField(item, RenameListBasicPropertyKeys.FileNameNumeric, "7");
            _AssertField(item, RenameListBasicPropertyKeys.FileNameLength, "15");
            _AssertField(item, RenameListBasicPropertyKeys.FullPathLength, "30");
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

            _AssertField(item, RenameListBasicPropertyKeys.ItemType, "Folder");
            _AssertField(item, RenameListBasicPropertyKeys.Name, "Album");
            _AssertField(item, RenameListBasicPropertyKeys.Extension, "");
            _AssertField(item, RenameListBasicPropertyKeys.FullName, "Album");
            _AssertField(item, RenameListBasicPropertyKeys.Folder, @"D:\Music");
            _AssertField(item, RenameListBasicPropertyKeys.FullPath, @"D:\Music\Album");
            _AssertField(item, RenameListBasicPropertyKeys.FileNameNumeric, "0");
            _AssertField(item, RenameListBasicPropertyKeys.FileNameLength, "5");
            _AssertField(item, RenameListBasicPropertyKeys.FullPathLength, "14");
        }

        [Fact]
        public void Resolve_preview_field_uses_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            item.Preview.Prefix = "after";

            var originalKey = RenameListFieldKey.Original(
                RenameListFieldCatalog.BasicGroupId,
                RenameListBasicPropertyKeys.FullName
            );
            var previewKey = RenameListFieldKey.Preview(
                RenameListFieldCatalog.BasicGroupId,
                RenameListBasicPropertyKeys.FullName
            );

            Assert.Equal("before.txt", RenameListFieldValueResolver.Resolve(item, originalKey));
            Assert.Equal("after.txt", RenameListFieldValueResolver.Resolve(item, previewKey));
        }

        [Fact]
        public void Resolve_unknown_field_throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var key = RenameListFieldKey.Original("Unknown", "Missing");

            Assert.Throws<ArgumentException>(() => RenameListFieldValueResolver.Resolve(item, key));
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
                RenameListFieldCatalog.BasicGroupId,
                RenameListBasicPropertyKeys.FileNameNumeric
            );

            Assert.Equal(expected, RenameListFieldValueResolver.Resolve(item, key));
        }

        private static void _AssertField(RenameItem item, string propertyKey, string expected)
        {
            var key = RenameListFieldKey.Original(RenameListFieldCatalog.BasicGroupId, propertyKey);
            Assert.Equal(expected, RenameListFieldValueResolver.Resolve(item, key));
        }
    }
}
