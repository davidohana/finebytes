using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.Views.GridColumnSizing;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// Resolves File List report-view auto-fit widths from row content.
    /// </summary>
    internal static class FileListReportColumnAutoFit
    {
        private const double _NameIconWidth = 16;
        private const double _NameIconSpacing = 4;
        private const double _SizeTrailingPadding = 8;

        /// <summary>
        /// Resolves the auto-fit pixel width for one report-view column.
        /// </summary>
        /// <param name="entries">Visible listing rows.</param>
        /// <param name="column">Grid column.</param>
        /// <returns>Clamped fit width in pixels.</returns>
        internal static int ResolveFitWidth(IEnumerable<FileListEntry> entries, DataGridColumn column)
        {
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(column);

            var headerText = column.Header as string;
            var minHeaderWidth = string.IsNullOrEmpty(headerText)
                ? 0
                : GridColumnTextWidths.GetMinimumHeaderWidth(headerText);

            var extraChrome = _GetExtraChrome(column);
            var maxCellWidth = 0;
            foreach (var entry in entries)
            {
                var cellWidth = GridColumnTextWidths.MeasureCellWidth(_GetCellText(entry, column), extraChrome);
                if (cellWidth > maxCellWidth)
                {
                    maxCellWidth = cellWidth;
                }
            }

            return GridColumnTextWidths.ClampFit(minHeaderWidth, maxCellWidth);
        }

        private static double _GetExtraChrome(DataGridColumn column)
        {
            if (column is DataGridTextColumn)
            {
                return GridColumnTextWidths.TextColumnCellMarginHorizontal;
            }

            if (column.SortMemberPath == "Name")
            {
                return _NameIconWidth + _NameIconSpacing;
            }

            if (column.SortMemberPath == "Length")
            {
                return _SizeTrailingPadding;
            }

            return 0;
        }

        private static string _GetCellText(FileListEntry entry, DataGridColumn column)
        {
            return column.SortMemberPath switch
            {
                "Name" => entry.Name,
                "LastWriteTime" => entry.DateModifiedDisplay,
                "Type" => entry.Type,
                "Length" => entry.SizeDisplay,
                _ => string.Empty,
            };
        }
    }
}
