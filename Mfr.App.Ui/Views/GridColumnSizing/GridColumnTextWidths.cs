using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Mfr.App.Ui.Views.GridColumnSizing
{
    /// <summary>
    /// Shared File List / Rename List grid text measurement for header min-widths and splitter auto-fit.
    /// </summary>
    internal static class GridColumnTextWidths
    {
        /// <summary>
        /// Upper bound for double-click header auto-fit so path columns do not consume the whole viewport.
        /// </summary>
        internal const int MaxAutoFitWidth = 960;

        /// <summary>
        /// Horizontal header padding matching <c>Padding="6,0,4,0"</c> on File List / Rename List column headers.
        /// </summary>
        internal const double HeaderHorizontalPadding = 10;

        /// <summary>
        /// Horizontal cell padding matching <c>Padding="4,0"</c> on File List / Rename List cells.
        /// </summary>
        internal const double CellPaddingHorizontal = 8;

        /// <summary>
        /// Horizontal TextBlock margin matching <c>DataGridTextColumnCellTextBlockMargin</c> (6+6).
        /// </summary>
        internal const double TextColumnCellMarginHorizontal = 12;

        private static readonly FontFamily _FontFamily = new("Segoe UI, SegoeUI");
        private const double _FontSize = 12;
        private const double _MeasurementSafetyBuffer = 8;
        private const double _TextWidthSlack = 1.02;

        /// <summary>
        /// Measures unwrapped text width in the File List / Rename List grid font.
        /// </summary>
        /// <param name="text">Text to measure.</param>
        /// <returns>Layout width in device-independent pixels.</returns>
        internal static double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var typeface = new Typeface(_FontFamily, FontStyle.Normal, FontWeight.Normal);
            using var layout = new TextLayout(text, typeface, _FontSize, null, maxWidth: double.PositiveInfinity);
            return layout.Width;
        }

        /// <summary>
        /// Gets the minimum pixel width needed to display a column header without truncation.
        /// </summary>
        /// <param name="headerText">Grid column header text.</param>
        /// <returns>Minimum column width in pixels.</returns>
        internal static int GetMinimumHeaderWidth(string headerText)
        {
            ArgumentException.ThrowIfNullOrEmpty(headerText);
            return (int)Math.Ceiling(MeasureText(headerText) + HeaderHorizontalPadding);
        }

        /// <summary>
        /// Measures the pixel width needed to display one cell value, including cell chrome.
        /// </summary>
        /// <param name="text">Cell display text.</param>
        /// <param name="extraChrome">Additional horizontal content such as icon width or extra padding.</param>
        /// <returns>Cell width in pixels.</returns>
        internal static int MeasureCellWidth(string text, double extraChrome = 0)
        {
            if (string.IsNullOrEmpty(text) && extraChrome <= 0)
            {
                return 0;
            }

            var textWidth = Math.Ceiling(MeasureText(text) * _TextWidthSlack);
            return (int)Math.Ceiling(textWidth + CellPaddingHorizontal + _MeasurementSafetyBuffer + extraChrome);
        }

        /// <summary>
        /// Clamps an auto-fit width to at least <paramref name="minHeaderWidth"/> and at most <see cref="MaxAutoFitWidth"/>.
        /// </summary>
        /// <param name="minHeaderWidth">Minimum width from the header label.</param>
        /// <param name="maxCellWidth">Widest measured cell.</param>
        /// <returns>Auto-fit width in pixels.</returns>
        internal static int ClampFit(int minHeaderWidth, int maxCellWidth)
        {
            return Math.Min(Math.Max(minHeaderWidth, maxCellWidth), MaxAutoFitWidth);
        }
    }
}
