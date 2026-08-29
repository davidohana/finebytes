using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Mfr.App.Ui.Views.GridColumnSizing;

namespace Mfr.Tests.Ui.GridColumnSizing
{
    /// <summary>
    /// Tests for Rename List / File List grid text measurement font context.
    /// </summary>
    public sealed class GridColumnTextWidthsTests
    {
        [Fact]
        public void ForRenameList_uses_GridFonts()
        {
            var proportional = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: false);
            var fixedWidth = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: true);

            Assert.Same(GridFonts.FileListFamily, proportional.FontFamily);
            Assert.Same(GridFonts.RenameListFixedWidthFamily, fixedWidth.FontFamily);
            Assert.Equal(GridFonts.FontSize, proportional.FontSize);
            Assert.Equal(GridFonts.FontSize, fixedWidth.FontSize);
        }

        /// <summary>
        /// Verifies theme keys registered at app init are the same <see cref="GridFonts"/> instances.
        /// </summary>
        [AvaloniaFact]
        public void Theme_resources_match_GridFonts()
        {
            var app = Application.Current;
            Assert.NotNull(app);
            var theme = app.ActualThemeVariant;

            Assert.True(app.TryGetResource("FileListFont", theme, out var fileListFont));
            Assert.Same(GridFonts.FileListFamily, Assert.IsType<FontFamily>(fileListFont));

            Assert.True(app.TryGetResource("RenameListFixedWidthFont", theme, out var fixedWidthFont));
            Assert.Same(GridFonts.RenameListFixedWidthFamily, Assert.IsType<FontFamily>(fixedWidthFont));

            Assert.True(app.TryGetResource("FileListFontSize", theme, out var fontSize));
            Assert.Equal(GridFonts.FontSize, Assert.IsType<double>(fontSize));

            Assert.True(app.TryGetResource("FileListSortGlyphFontSize", theme, out var glyphSize));
            Assert.Equal(GridFonts.SortGlyphFontSize, Assert.IsType<double>(glyphSize));
        }
    }
}
