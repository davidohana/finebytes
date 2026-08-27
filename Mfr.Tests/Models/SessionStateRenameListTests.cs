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
            Assert.Equal(
                [
                    new RenameListSortKey(RenameListSortColumn.FileFolder),
                    new RenameListSortKey(RenameListSortColumn.FullPath),
                ],
                RenameListSortKey.DefaultKeys
            );

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
                    new SessionStateRenameListSortField(RenameListSortColumn.FullPath),
                ],
                SessionStateRenameList.FromSortKeys(RenameListSortKey.DefaultKeys)
            );
        }
    }
}
