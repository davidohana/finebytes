using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="SessionStateRenameList"/> sort/column session JSON shapes.
    /// </summary>
    public sealed class SessionStateRenameListTests
    {
        [Fact]
        public void Sort_fields_round_trip_via_session_store()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-sort-" + Guid.NewGuid() + ".json");
            try
            {
                SessionStore.Save(
                    new SessionState
                    {
                        RenameList = new SessionStateRenameList
                        {
                            SortFields =
                            [
                                new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true),
                            ],
                        },
                    },
                    path
                );

                var loaded = SessionStore.Load(path);
                Assert.Equal(
                    [new RenameListSortKey(RenameListTestHelpers.ParentFolderKey, Descending: true)],
                    loaded.RenameList?.SortFields
                );

                SessionStore.Save(
                    new SessionState { RenameList = new SessionStateRenameList { SortFields = [] } },
                    path
                );
                Assert.Empty(SessionStore.Load(path).RenameList!.SortFields!);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
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
                SessionStore.Save(
                    new SessionState { RenameList = new SessionStateRenameList { VisibleColumns = sessionColumns } },
                    path
                );

                var loaded = SessionStore.Load(path);
                Assert.NotNull(loaded.RenameList?.VisibleColumns);
                Assert.Equal(2, loaded.RenameList.VisibleColumns.Count);
                Assert.Equal(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                    loaded.RenameList.VisibleColumns[0].Key
                );
                Assert.Equal(220, loaded.RenameList.VisibleColumns[0].Width);
                Assert.Equal(previewKey, loaded.RenameList.VisibleColumns[1].Key);
                Assert.Null(loaded.RenameList.VisibleColumns[1].Width);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
