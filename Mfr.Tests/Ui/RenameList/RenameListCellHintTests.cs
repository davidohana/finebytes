using Avalonia.Media;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        /// <summary>
        /// Verifies hints use bold column name, colon, then value.
        /// </summary>
        [Fact]
        public void FormatParts_Uses_Bold_Column_Name()
        {
            var hint = RenameListCellHint.FormatParts("Full File Name", "alpha.txt");
            Assert.Equal(2, hint.Runs.Count);
            Assert.Equal("Full File Name", hint.Runs[0].Text);
            Assert.Equal(FontWeight.Bold, hint.Runs[0].FontWeight);
            Assert.Equal(": alpha.txt", hint.Runs[1].Text);
            Assert.Equal("Full File Name: alpha.txt", hint.ToPlainText());
        }
    }
}
