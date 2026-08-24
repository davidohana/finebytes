using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        /// <summary>
        /// Verifies original and preview columns use MFR7-style prefixes.
        /// </summary>
        [Fact]
        public void Format_Uses_Original_And_Preview_Prefixes()
        {
            Assert.Equal(
                "[Original Full File Name] alpha.txt",
                RenameListCellHint.Format("Full File Name", "alpha.txt", false)
            );
            Assert.Equal(
                "[Preview Full File Name (Preview)] beta.txt",
                RenameListCellHint.Format("Full File Name (Preview)", "beta.txt", true)
            );
        }

        /// <summary>
        /// Verifies preview columns are detected by header text.
        /// </summary>
        [Fact]
        public void IsPreviewColumn_Recognizes_Preview_Header()
        {
            Assert.True(RenameListCellHint.IsPreviewColumn("Full File Name (Preview)"));
            Assert.False(RenameListCellHint.IsPreviewColumn("Full File Name"));
        }
    }
}
