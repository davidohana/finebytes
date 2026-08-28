using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.GridColumnSizing;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Minimum Rename List grid column widths so header text is not truncated.
    /// </summary>
    internal static class RenameListGridColumnWidths
    {
        /// <summary>
        /// Upper bound for double-click header auto-fit so path columns do not consume the whole viewport.
        /// </summary>
        internal const int MaxAutoFitWidth = GridColumnTextWidths.MaxAutoFitWidth;

        private static readonly FontFamily _GlyphFontFamily = new("Segoe UI, SegoeUI");
        private const double _SortGlyphFontSize = 11; // FileListSortGlyphFontSize
        private const double _SortGlyphMarginLeft = 6;
        private const double _SortGlyphBorderHorizontal = 2; // BorderThickness="1"
        private const double _SortGlyphPaddingHorizontal = 8; // Padding="4,0"
        private const double _SortGlyphStackSpacing = 1;
        private const double _PreviewGlyphFontSize = 11;
        private const double _PreviewGlyphSpacing = 4;
        private const double _PreviewGlyphMarginLeft = 0;
        private const double _PreviewGlyphBorderHorizontal = 2;
        private const double _PreviewGlyphPaddingHorizontal = 8;

        /// <summary>
        /// Gets the minimum pixel width needed to display a column header without truncation.
        /// </summary>
        /// <param name="headerText">Grid column header text.</param>
        /// <param name="reserveSortGlyph">
        /// When <see langword="true"/>, reserves space for the Auto-Sort priority/direction glyph on original columns.
        /// </param>
        /// <param name="reservePreviewGlyph">
        /// When <see langword="true"/>, reserves space for the preview badge on preview columns.
        /// </param>
        /// <returns>Minimum column width in pixels.</returns>
        public static int GetMinimumHeaderWidth(
            string headerText,
            bool reserveSortGlyph = false,
            bool reservePreviewGlyph = false
        )
        {
            var width = GridColumnTextWidths.GetMinimumHeaderWidth(headerText);
            if (reserveSortGlyph)
            {
                width += (int)Math.Ceiling(_MeasureSortGlyphReserve());
            }

            if (reservePreviewGlyph)
            {
                width += (int)Math.Ceiling(_MeasurePreviewGlyphReserve());
            }

            return width;
        }

        /// <summary>
        /// Gets the pixel width to fit visible cell values for one column, clamped to <see cref="MaxAutoFitWidth"/>.
        /// </summary>
        /// <param name="entries">Grid rows.</param>
        /// <param name="fieldKey">Column field key.</param>
        /// <param name="minHeaderWidth">Minimum width from the header label.</param>
        /// <returns>Auto-fit width in pixels.</returns>
        public static int GetAutoFitWidth(
            IEnumerable<RenameListEntry> entries,
            RenameListFieldKey fieldKey,
            int minHeaderWidth
        )
        {
            ArgumentNullException.ThrowIfNull(entries);

            var maxCellWidth = 0;
            foreach (var entry in entries)
            {
                var cellWidth = GridColumnTextWidths.MeasureCellWidth(
                    entry.GetFieldText(fieldKey),
                    GridColumnTextWidths.TextColumnCellMarginHorizontal
                );
                if (cellWidth > maxCellWidth)
                {
                    maxCellWidth = cellWidth;
                }
            }

            return GridColumnTextWidths.ClampFit(minHeaderWidth, maxCellWidth);
        }

        private static double _MeasureSortGlyphReserve()
        {
            var typeface = new Typeface(_GlyphFontFamily, FontStyle.Normal, FontWeight.Normal);
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

        private static double _MeasurePreviewGlyphReserve()
        {
            var typeface = new Typeface(_GlyphFontFamily, FontStyle.Normal, FontWeight.Normal);
            using var badgeLayout = new TextLayout(
                RenameListPreviewGlyph.Text,
                typeface,
                _PreviewGlyphFontSize,
                null,
                maxWidth: double.PositiveInfinity
            );

            var contentWidth = badgeLayout.Width;
            return _PreviewGlyphSpacing
                + _PreviewGlyphMarginLeft
                + _PreviewGlyphBorderHorizontal
                + _PreviewGlyphPaddingHorizontal
                + contentWidth;
        }
    }
}
