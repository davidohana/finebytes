using Mfr.Models.Config;
using Mfr.Models.Rename;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="RenameListSortKey"/> session field conversion.
    /// </summary>
    public sealed class RenameListSortKeyTests
    {
        [Fact]
        public void Session_fields_round_trip_default_and_desc()
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
                RenameListSortKey.FromSessionFields(descending)
            );
            Assert.Empty(RenameListSortKey.FromSessionFields([]));
            Assert.Equal(RenameListSortKey.DefaultSessionFields, RenameListSortKey.ToSessionFields(RenameListSortKey.DefaultKeys));
        }
    }
}
