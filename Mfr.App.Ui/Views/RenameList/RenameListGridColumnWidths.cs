using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Minimum Rename List grid column widths so header text is not truncated.
    /// </summary>
    internal static class RenameListGridColumnWidths
    {
        // Matches FileList.axaml / RenameListView.axaml column header styling.
        private static readonly FontFamily _HeaderFontFamily = new("Segoe UI, SegoeUI");
        private const double _HeaderFontSize = 12;
        private const double _HeaderHorizontalPadding = 10; // Padding="6,0,4,0"
        private const double _SortGlyphFontSize = 11; // FileListSortGlyphFontSize
        private const double _SortGlyphMarginLeft = 6;
        private const double _SortGlyphBorderHorizontal = 2; // BorderThickness="1"
        private const double _SortGlyphPaddingHorizontal = 8; // Padding="4,0"
        private const double _SortGlyphStackSpacing = 1;

        /// <summary>
        /// Gets the minimum pixel width needed to display a column header without truncation.
        /// </summary>
        /// <param name="headerText">Grid column header text.</param>
        /// <param name="reserveSortGlyph">
        /// When <see langword="true"/>, reserves space for the Auto-Sort priority/direction glyph on original columns.
        /// </param>
        /// <returns>Minimum column width in pixels.</returns>
        public static int GetMinimumHeaderWidth(string headerText, bool reserveSortGlyph = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(headerText);

            var typeface = new Typeface(_HeaderFontFamily, FontStyle.Normal, FontWeight.Normal);
            using var layout = new TextLayout(
                headerText,
                typeface,
                _HeaderFontSize,
                null,
                maxWidth: double.PositiveInfinity
            );

            var extra = _HeaderHorizontalPadding + (reserveSortGlyph ? _MeasureSortGlyphReserve() : 0);
            return (int)Math.Ceiling(layout.Width + extra);
        }

        private static double _MeasureSortGlyphReserve()
        {
            var typeface = new Typeface(_HeaderFontFamily, FontStyle.Normal, FontWeight.Normal);
            using var priorityLayout = new TextLayout(
                "10",
                typeface,
                _SortGlyphFontSize,
                null,
                maxWidth: double.PositiveInfinity
            );
            using var directionLayout = new TextLayout(
                "↓",
                typeface,
                _SortGlyphFontSize,
                null,
                maxWidth: double.PositiveInfinity
            );

            var contentWidth = priorityLayout.Width + _SortGlyphStackSpacing + directionLayout.Width;
            return _SortGlyphMarginLeft + _SortGlyphBorderHorizontal + _SortGlyphPaddingHorizontal + contentWidth;
        }
    }
}
