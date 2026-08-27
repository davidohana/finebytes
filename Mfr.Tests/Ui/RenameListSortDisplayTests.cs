using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List Auto-Sort tooltip and column label formatting.
    /// </summary>
    public sealed class RenameListSortDisplayTests
    {
        /// <summary>
        /// Verifies column labels match grid headers.
        /// </summary>
        [Fact]
        public void GetColumnLabel_Matches_Grid_Headers()
        {
            Assert.Equal("File/Folder", RenameListSortDisplay.GetColumnLabel(RenameListSortColumn.FileFolder));
            Assert.Equal("Parent Folder", RenameListSortDisplay.GetColumnLabel(RenameListSortColumn.ParentFolder));
            Assert.Equal("Full File Name", RenameListSortDisplay.GetColumnLabel(RenameListSortColumn.FullFileName));
            Assert.Equal("Full Path", RenameListSortDisplay.GetColumnLabel(RenameListSortColumn.FullPath));
        }

        /// <summary>
        /// Verifies an empty key list shows the off tooltip.
        /// </summary>
        [Fact]
        public void FormatSummary_Empty_Shows_Off_Message()
        {
            Assert.Equal(RenameListSortDisplay.AutoSortOffSummary, RenameListSortDisplay.FormatSummary([]));
        }

        /// <summary>
        /// Verifies default keys format as numbered ascending lines.
        /// </summary>
        [Fact]
        public void FormatSummary_DefaultKeys_Shows_Numbered_Lines()
        {
            Assert.Equal(
                "1. File/Folder ↑\n2. Parent Folder ↑\n3. Full File Name ↑",
                RenameListSortDisplay.FormatSummary(RenameListSortKey.DefaultKeys)
            );
        }

        /// <summary>
        /// Verifies a single descending key formats with a down arrow.
        /// </summary>
        [Fact]
        public void FormatSummary_SingleColumn_Shows_Direction()
        {
            Assert.Equal(
                "1. Full File Name ↓",
                RenameListSortDisplay.FormatSummary([
                    new RenameListSortKey(RenameListSortColumn.FullFileName, Descending: true),
                ])
            );
        }
    }
}
