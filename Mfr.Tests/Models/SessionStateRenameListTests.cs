using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="SessionStateRenameList"/> sort/column session JSON shapes.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class SessionStateRenameListTests
    {
        [Fact]
        public void Sort_fields_round_trip_via_config_store_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-sort-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.RenameList = new SessionStateRenameList
                {
                    SortFields = [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                };
                ConfigStore.Save(path);

                ConfigStore.Load(path);
                Assert.Equal(
                    [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                    ConfigStore.RenameList?.SortFields
                );

                ConfigStore.RenameList = new SessionStateRenameList { SortFields = [] };
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
                ConfigStore.RenameList = new SessionStateRenameList { VisibleColumns = sessionColumns };
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
    }
}
