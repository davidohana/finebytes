using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="SessionStateRenameList"/> sort-field conversion.
    /// </summary>
    public sealed class SessionStateRenameListTests
    {
        [Fact]
        public void Sort_fields_round_trip_default_and_desc()
        {
            var descending = new List<SessionStateRenameListSortField>
            {
                new(RenameListSortColumn.ParentFolder, Descending: true),
            };
            Assert.Equal(
                [new RenameListSortKey(RenameListSortColumn.ParentFolder, Descending: true)],
                SessionStateRenameList.ToSortKeys(descending)
            );
            Assert.Empty(SessionStateRenameList.ToSortKeys([]));
            Assert.Equal(
                [
                    new SessionStateRenameListSortField(RenameListSortColumn.FileFolder),
                    new SessionStateRenameListSortField(RenameListSortColumn.ParentFolder),
                    new SessionStateRenameListSortField(RenameListSortColumn.FullFileName),
                ],
                SessionStateRenameList.FromSortKeys(RenameListSortKey.DefaultKeys)
            );
        }

        [Fact]
        public void Visible_columns_round_trip_keys_and_widths()
        {
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);
            var sessionColumns = new List<SessionStateRenameListColumn>
            {
                new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicFullPathField.Key), Width: 220),
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
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicFullPathField.Key),
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
