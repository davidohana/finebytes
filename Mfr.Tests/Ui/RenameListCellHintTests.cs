using Avalonia.Media;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        /// <summary>
        /// Verifies hints use bold column name, colon, then value, including preview suffix stripping.
        /// </summary>
        [Fact]
        public void FormatParts_Uses_Bold_Column_Name_And_Strips_Preview_Suffix()
        {
            var plain = RenameListCellHint.FormatParts("Full File Name", "alpha.txt", isPreviewColumn: false);
            Assert.Equal(2, plain.Runs.Count);
            Assert.Equal("Full File Name", plain.Runs[0].Text);
            Assert.Equal(FontWeight.Bold, plain.Runs[0].FontWeight);
            Assert.Equal(": alpha.txt", plain.Runs[1].Text);
            Assert.Equal("Full File Name: alpha.txt", plain.ToPlainText());

            var preview = RenameListCellHint.FormatParts("Full File Name (Preview)", "beta.txt", isPreviewColumn: true);
            Assert.Equal("Full File Name", preview.Runs[0].Text);
            Assert.Equal(": beta.txt", preview.Runs[1].Text);
            Assert.Equal("Full File Name: beta.txt", preview.ToPlainText());
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

        /// <summary>
        /// Verifies templated sort columns resolve labels from sort member paths.
        /// </summary>
        [Fact]
        public void GetColumnHeader_Resolves_SortMemberPath_When_Header_Is_Templated()
        {
            Assert.Equal(
                "Full File Name",
                RenameListCellHint.GetColumnHeader(nameof(RenameListEntry.FullFileName), null)
            );
            Assert.Equal(
                "Full File Name (Preview)",
                RenameListCellHint.GetColumnHeader(null, "Full File Name (Preview)")
            );
        }
    }
}
