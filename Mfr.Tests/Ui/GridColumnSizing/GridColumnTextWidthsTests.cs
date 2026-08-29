using Mfr.App.Ui.Views.GridColumnSizing;

namespace Mfr.Tests.Ui.GridColumnSizing
{
    /// <summary>
    /// Tests for Rename List / File List grid text measurement font context.
    /// </summary>
    public sealed class GridColumnTextWidthsTests
    {
        [Fact]
        public void ForRenameList_selects_distinct_font_families()
        {
            var proportional = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: false);
            var fixedWidth = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: true);

            Assert.NotEqual(proportional.FontFamily.Name, fixedWidth.FontFamily.Name);
            Assert.Contains("Segoe", proportional.FontFamily.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mono", fixedWidth.FontFamily.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
