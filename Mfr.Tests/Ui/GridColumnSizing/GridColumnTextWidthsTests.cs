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
        public void ForRenameList_uses_AppChromeFonts()
        {
            var proportional = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: false);
            var fixedWidth = GridColumnTextFontContext.ForRenameList(useFixedWidthFont: true);

            Assert.Same(AppChromeFonts.AppChromeFamily, proportional.FontFamily);
            Assert.Same(AppChromeFonts.AppChromeFixedWidthFamily, fixedWidth.FontFamily);
            Assert.Equal(AppChromeFonts.FontSize, proportional.FontSize);
            Assert.Equal(AppChromeFonts.FontSize, fixedWidth.FontSize);
        }

        /// <summary>
        /// Verifies theme keys registered at app init are the same <see cref="AppChromeFonts"/> instances.
        /// </summary>
        [AvaloniaFact]
        public void Theme_resources_match_AppChromeFonts()
        {
            var app = Application.Current;
            Assert.NotNull(app);
            var theme = app.ActualThemeVariant;

            Assert.True(app.TryGetResource("AppChromeFont", theme, out var appChromeFont));
            Assert.Same(AppChromeFonts.AppChromeFamily, Assert.IsType<FontFamily>(appChromeFont));

            Assert.True(app.TryGetResource("AppChromeFixedWidthFont", theme, out var fixedWidthFont));
            Assert.Same(AppChromeFonts.AppChromeFixedWidthFamily, Assert.IsType<FontFamily>(fixedWidthFont));

            Assert.True(app.TryGetResource("AppChromeFontSize", theme, out var fontSize));
            Assert.Equal(AppChromeFonts.FontSize, Assert.IsType<double>(fontSize));

            Assert.True(app.TryGetResource("AppChromeSortGlyphFontSize", theme, out var glyphSize));
            Assert.Equal(AppChromeFonts.SortGlyphFontSize, Assert.IsType<double>(glyphSize));
        }
    }
}
