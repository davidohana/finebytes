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
        public void FormatSummary_off_default_and_single_column()
        {
            Assert.Equal(RenameListSortDisplay.AutoSortOffSummary, RenameListSortDisplay.FormatSummary([]));

            Assert.Equal(
                "1. File/Folder ↑\n2. Parent Folder ↑\n3. Full File Name ↑",
                RenameListSortDisplay.FormatSummary(RenameListSortKey.DefaultKeys)
            );

            Assert.Equal(
                "1. Full File Name ↓",
                RenameListSortDisplay.FormatSummary([
                    new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true),
                ])
            );

            Assert.Equal("Full File Path", RenameListSortDisplay.GetFieldLabel(RenameListTestHelpers.FullPathKey));
        }

        [Fact]
        public void BuildColumnSortStates_DefaultKeys_Assigns_Priorities_1_2_3()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates(RenameListSortKey.DefaultKeys);

            Assert.Equal(1, states[RenameListTestHelpers.FileFolderKey].Priority);

            Assert.Equal(2, states[RenameListTestHelpers.ParentFolderKey].Priority);

            Assert.Equal(3, states[RenameListTestHelpers.FullFileNameKey].Priority);

            Assert.False(states[RenameListTestHelpers.FileFolderKey].IsDescending);

            Assert.False(states[RenameListTestHelpers.ParentFolderKey].IsDescending);

            Assert.False(states[RenameListTestHelpers.FullFileNameKey].IsDescending);
        }

        [Fact]
        public void BuildColumnSortStates_SingleColumn_Only_One_Active()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates([
                new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true),
            ]);

            Assert.False(states[RenameListTestHelpers.FileFolderKey].IsActive);

            Assert.False(states[RenameListTestHelpers.ParentFolderKey].IsActive);

            Assert.True(states[RenameListTestHelpers.FullFileNameKey].IsActive);

            Assert.Equal(1, states[RenameListTestHelpers.FullFileNameKey].Priority);

            Assert.True(states[RenameListTestHelpers.FullFileNameKey].IsDescending);

            Assert.Equal("↓", states[RenameListTestHelpers.FullFileNameKey].DirectionGlyph);
        }

        [Fact]
        public void BuildColumnSortStates_FullPathKey_Does_Not_Activate_Other_Columns()
        {
            var states = RenameListSortDisplay.BuildColumnSortStates([
                new RenameListSortKey(RenameListTestHelpers.FullPathKey),
            ]);

            Assert.True(states[RenameListTestHelpers.FullPathKey].IsActive);

            Assert.Equal(1, states[RenameListTestHelpers.FullPathKey].Priority);

            Assert.False(states[RenameListTestHelpers.FileFolderKey].IsActive);

            Assert.False(states[RenameListTestHelpers.ParentFolderKey].IsActive);

            Assert.False(states[RenameListTestHelpers.FullFileNameKey].IsActive);
        }
    }
}
