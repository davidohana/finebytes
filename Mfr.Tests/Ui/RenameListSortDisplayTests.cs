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

        /// <summary>
        /// Verifies default keys assign priorities 1–3 to visible columns.
        /// </summary>
        [Fact]
        public void BuildColumnSortStates_DefaultKeys_Assigns_Priorities_1_2_3()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates(RenameListSortKey.DefaultKeys);

            Assert.Equal(1, states.FileFolder.Priority);
            Assert.Equal(2, states.ParentFolder.Priority);
            Assert.Equal(3, states.FullFileName.Priority);
            Assert.False(states.FileFolder.IsDescending);
            Assert.False(states.ParentFolder.IsDescending);
            Assert.False(states.FullFileName.IsDescending);
        }

        /// <summary>
        /// Verifies a single-column sort shows priority 1 on that column only.
        /// </summary>
        [Fact]
        public void BuildColumnSortStates_SingleColumn_Only_One_Active()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates([
                new RenameListSortKey(RenameListSortColumn.FullFileName, Descending: true),
            ]);

            Assert.False(states.FileFolder.IsActive);
            Assert.False(states.ParentFolder.IsActive);
            Assert.True(states.FullFileName.IsActive);
            Assert.Equal(1, states.FullFileName.Priority);
            Assert.True(states.FullFileName.IsDescending);
            Assert.Equal("↓", states.FullFileName.DirectionGlyph);
        }

        /// <summary>
        /// Verifies a Full Path key does not produce a visible-column header glyph.
        /// </summary>
        [Fact]
        public void BuildColumnSortStates_FullPathKey_Has_No_Visible_Header()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates([
                new RenameListSortKey(RenameListSortColumn.FullPath),
            ]);

            Assert.False(states.FileFolder.IsActive);
            Assert.False(states.ParentFolder.IsActive);
            Assert.False(states.FullFileName.IsActive);
        }
    }
}
