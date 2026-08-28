using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Rename List Auto-Sort tooltip and column label formatting.
    /// </summary>
    public sealed class RenameListSortDisplayTests
    {
        /// <summary>
        /// Verifies Auto-Sort summary text for off, default keys, and single-column sort.
        /// </summary>
        [Fact]
        public void FormatSummary_Reflects_Key_State()
        {
            Assert.Equal(RenameListSortDisplay.AutoSortOffSummary, RenameListSortDisplay.FormatSummary([]));

            Assert.Equal(
                "1. File/Folder ↑\n2. Parent Folder ↑\n3. Full File Name ↑",
                RenameListSortDisplay.FormatSummary(RenameListSortKey.DefaultKeys)
            );

            Assert.Equal(
                "1. Full File Name ↓",
                RenameListSortDisplay.FormatSummary([
                    new RenameListSortKey(RenameListSortColumn.FullFileName, Descending: true),
                ])
            );
        }

        /// <summary>
        /// Verifies default keys assign priorities 1–3 by column.
        /// </summary>
        [Fact]
        public void BuildColumnSortStates_DefaultKeys_Assigns_Priorities_1_2_3()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates(RenameListSortKey.DefaultKeys);

            Assert.Equal(1, states[RenameListSortColumn.FileFolder].Priority);

            Assert.Equal(2, states[RenameListSortColumn.ParentFolder].Priority);

            Assert.Equal(3, states[RenameListSortColumn.FullFileName].Priority);

            Assert.False(states[RenameListSortColumn.FileFolder].IsDescending);

            Assert.False(states[RenameListSortColumn.ParentFolder].IsDescending);

            Assert.False(states[RenameListSortColumn.FullFileName].IsDescending);
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

            Assert.False(states[RenameListSortColumn.FileFolder].IsActive);

            Assert.False(states[RenameListSortColumn.ParentFolder].IsActive);

            Assert.True(states[RenameListSortColumn.FullFileName].IsActive);

            Assert.Equal(1, states[RenameListSortColumn.FullFileName].Priority);

            Assert.True(states[RenameListSortColumn.FullFileName].IsDescending);

            Assert.Equal("↓", states[RenameListSortColumn.FullFileName].DirectionGlyph);
        }

        /// <summary>
        /// Verifies a Full Path key is stored and does not mark other columns active.
        /// </summary>
        [Fact]
        public void BuildColumnSortStates_FullPathKey_Does_Not_Activate_Other_Columns()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates([
                new RenameListSortKey(RenameListSortColumn.FullPath),
            ]);

            Assert.True(states[RenameListSortColumn.FullPath].IsActive);

            Assert.Equal(1, states[RenameListSortColumn.FullPath].Priority);

            Assert.False(states[RenameListSortColumn.FileFolder].IsActive);

            Assert.False(states[RenameListSortColumn.ParentFolder].IsActive);

            Assert.False(states[RenameListSortColumn.FullFileName].IsActive);
        }

        /// <summary>
        /// Verifies XAML-style string keys resolve the same as enum keys.
        /// </summary>
        [Fact]
        public void ColumnSortStates_StringIndexer_Matches_Enum()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates(RenameListSortKey.DefaultKeys);

            Assert.Equal(states[RenameListSortColumn.FileFolder], states["FileFolder"]);

            Assert.False(states["Unknown"].IsActive);
        }

        /// <summary>
        /// Verifies header SortMemberPath maps to visible columns only.
        /// </summary>
        [Fact]
        public void TryMapMemberPath_Maps_Visible_Columns()
        {
            Assert.True(RenameListSortDisplay.TryMapMemberPath(nameof(RenameListEntry.FileFolder), out var fileFolder));
            Assert.Equal(RenameListSortColumn.FileFolder, fileFolder);

            Assert.True(RenameListSortDisplay.TryMapMemberPath(nameof(RenameListEntry.ParentFolder), out var parent));
            Assert.Equal(RenameListSortColumn.ParentFolder, parent);

            Assert.True(RenameListSortDisplay.TryMapMemberPath(nameof(RenameListEntry.FullFileName), out var name));
            Assert.Equal(RenameListSortColumn.FullFileName, name);

            Assert.False(RenameListSortDisplay.TryMapMemberPath(nameof(RenameListEntry.FullPath), out _));
            Assert.False(RenameListSortDisplay.TryMapMemberPath(null, out _));
        }
    }
}
