namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests <see cref="RenameListSortKey"/> encode/decode.
    /// </summary>
    public sealed class RenameListSortKeyTests
    {
        [Fact]
        public void Parse_default_and_desc()
        {
            var keys = RenameListSortKey.Parse(RenameListSortKey.Default);
            Assert.Equal(
                [
                    new RenameListSortKey(RenameListSortColumn.FileFolder),
                    new RenameListSortKey(RenameListSortColumn.FullPath),
                ],
                keys
            );

            Assert.Equal(
                [new RenameListSortKey(RenameListSortColumn.ParentFolder, Descending: true)],
                RenameListSortKey.Parse("parentFolder:desc")
            );
            Assert.Empty(RenameListSortKey.Parse(string.Empty));
            Assert.Empty(RenameListSortKey.Parse(null));
            Assert.Equal("FileFolder,FullPath", RenameListSortKey.Format(keys));
        }
    }
}
