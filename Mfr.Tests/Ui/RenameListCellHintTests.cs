using Avalonia.Media;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        /// <summary>
        /// Verifies original hints bold the column header run.
        /// </summary>
        [Fact]
        public void FormatParts_Bolds_Original_Column_Header()
        {
            var hint = RenameListCellHint.FormatParts("Full File Name", "alpha.txt", isPreviewColumn: false);

            Assert.Equal(3, hint.Runs.Count);
            Assert.Equal("[Original ", hint.Runs[0].Text);
            Assert.Equal("Full File Name", hint.Runs[1].Text);
            Assert.Equal(FontWeight.Bold, hint.Runs[1].FontWeight);
            Assert.Equal("] alpha.txt", hint.Runs[2].Text);
            Assert.Equal("[Original Full File Name] alpha.txt", hint.ToPlainText());
        }

        /// <summary>
        /// Verifies preview hints color the kind label and bold the column header.
        /// </summary>
        [Fact]
        public void FormatParts_Colors_Preview_Kind_And_Bolds_Column_Header()
        {
            var hint = RenameListCellHint.FormatParts("Full File Name (Preview)", "beta.txt", isPreviewColumn: true);

            Assert.Equal(5, hint.Runs.Count);
            Assert.Equal("[", hint.Runs[0].Text);
            Assert.Equal("Preview", hint.Runs[1].Text);
            Assert.Equal(RenameListCellHint.PreviewKindBrushKey, hint.Runs[1].ForegroundResourceKey);
            Assert.Equal("Full File Name (Preview)", hint.Runs[3].Text);
            Assert.Equal(FontWeight.Bold, hint.Runs[3].FontWeight);
            Assert.Equal("[Preview Full File Name (Preview)] beta.txt", hint.ToPlainText());
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
        /// Verifies plain hints are a single default run.
        /// </summary>
        [Fact]
        public void StatusHintDisplay_FromPlain_Uses_Single_Run()
        {
            var hint = StatusHintDisplay.FromPlain("Added 3 item(s).");

            Assert.Single(hint.Runs);
            Assert.Equal("Added 3 item(s).", hint.Runs[0].Text);
            Assert.Null(hint.Runs[0].FontWeight);
            Assert.Null(hint.Runs[0].ForegroundResourceKey);
            Assert.False(hint.IsEmpty);
        }
    }
}
