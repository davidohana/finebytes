using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="RenameListPrefs"/> sort/column session JSON shapes.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameListPrefsTests
    {
        [Fact]
        public void Sort_fields_round_trip_via_config_store_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-sort-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.RenameList = new RenameListPrefs
                {
                    SortFields = [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                };
                ConfigStore.Save(path);

                ConfigStore.Load(path);
                Assert.Equal(
                    [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                    ConfigStore.RenameList?.SortFields
                );

                ConfigStore.RenameList = new RenameListPrefs { SortFields = [] };
                ConfigStore.Save(path);
                ConfigStore.Load(path);
                Assert.Empty(ConfigStore.RenameList.SortFields);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Visible_columns_round_trip_keys_and_widths()
        {
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var sessionColumns = new List<RenameListVisibleColumnSpec>
            {
                new(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                    Width: 220
                ),
                new(previewKey),
            };

            var path = Path.Combine(Path.GetTempPath(), "mfr-session-columns-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.RenameList = new RenameListPrefs { VisibleColumns = sessionColumns };
                ConfigStore.Save(path);

                ConfigStore.Load(path);
                Assert.NotNull(ConfigStore.RenameList?.VisibleColumns);
                Assert.Equal(2, ConfigStore.RenameList.VisibleColumns.Count);
                Assert.Equal(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                    ConfigStore.RenameList.VisibleColumns[0].Key
                );
                Assert.Equal(220, ConfigStore.RenameList.VisibleColumns[0].Width);
                Assert.Equal(previewKey, ConfigStore.RenameList.VisibleColumns[1].Key);
                Assert.Null(ConfigStore.RenameList.VisibleColumns[1].Width);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Column_widths_round_trip_via_config_store_session()
        {
            var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var remembered = new List<RenameListVisibleColumnSpec> { new(nameKey, Width: 175) };

            var path = Path.Combine(Path.GetTempPath(), "mfr-session-col-widths-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.RenameList = new RenameListPrefs { ColumnWidths = remembered };
                ConfigStore.Save(path);

                ConfigStore.Load(path);
                Assert.NotNull(ConfigStore.RenameList?.ColumnWidths);
                Assert.Single(ConfigStore.RenameList.ColumnWidths);
                Assert.Equal(nameKey, ConfigStore.RenameList.ColumnWidths[0].Key);
                Assert.Equal(175, ConfigStore.RenameList.ColumnWidths[0].Width);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Ab_mode_prefs_round_trip_defaults_and_values()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-ab-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                Assert.False(new RenameListPrefs().AbModeEnabled);
                Assert.Equal(RenameListPrefs.AbSidePreview, new RenameListPrefs().AbSide);

                ConfigStore.RenameList = new RenameListPrefs
                {
                    AbModeEnabled = true,
                    AbSide = RenameListPrefs.AbSideOriginal,
                };
                ConfigStore.Save(path);

                ConfigStore.Load(path);
                Assert.True(ConfigStore.RenameList?.AbModeEnabled);
                Assert.Equal(RenameListPrefs.AbSideOriginal, ConfigStore.RenameList?.AbSide);

                File.WriteAllText(
                    path, /*lang=json,strict*/
                    """{"renameList":{}}"""
                );
                ConfigStore.Load(path);
                Assert.False(ConfigStore.RenameList?.AbModeEnabled);
                Assert.Equal(RenameListPrefs.AbSidePreview, ConfigStore.RenameList?.AbSide);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void NormalizeAbSide_maps_invalid_to_preview()
        {
            Assert.Equal(RenameListPrefs.AbSideOriginal, RenameListPrefs.NormalizeAbSide("original"));
            Assert.Equal(RenameListPrefs.AbSidePreview, RenameListPrefs.NormalizeAbSide("preview"));
            Assert.Equal(RenameListPrefs.AbSidePreview, RenameListPrefs.NormalizeAbSide(null));
            Assert.Equal(RenameListPrefs.AbSidePreview, RenameListPrefs.NormalizeAbSide(""));
            Assert.Equal(RenameListPrefs.AbSidePreview, RenameListPrefs.NormalizeAbSide("bogus"));
            Assert.Equal(RenameListPrefs.AbSidePreview, RenameListPrefs.NormalizeAbSide("Original"));
        }
    }
}
