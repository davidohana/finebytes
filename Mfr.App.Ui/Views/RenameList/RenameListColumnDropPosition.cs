using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Interprets Rename List column-reorder drop marker positions.
    /// </summary>
    internal static class RenameListColumnDropPosition
    {
        private const double EdgeTolerance = 0.5;

        /// <summary>
        /// Returns whether the drop marker is at the trailing edge after the last visible column.
        /// </summary>
        /// <param name="grid">Owning grid.</param>
        /// <param name="presenter">Column headers presenter hosting the drop marker.</param>
        /// <param name="dropOffset">Drop marker offset from the presenter left edge.</param>
        /// <returns>
        /// <see langword="true"/> when the marker is at or beyond the last visible column's right edge.
        /// </returns>
        public static bool IsAppendAtEnd(DataGrid grid, DataGridColumnHeadersPresenter presenter, double dropOffset)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(presenter);

            var headers = _GetReorderableHeaders(grid, presenter);
            if (headers.Count == 0)
            {
                return true;
            }

            var lastHeader = headers[^1];
            var lastRight = lastHeader.Bounds.Left + lastHeader.Bounds.Width;
            return dropOffset >= lastRight - EdgeTolerance;
        }

        private static List<DataGridColumnHeader> _GetReorderableHeaders(
            DataGrid grid,
            DataGridColumnHeadersPresenter presenter
        )
        {
            return
            [
                .. presenter
                    .GetVisualChildren()
                    .OfType<DataGridColumnHeader>()
                    .Where(header =>
                        header.IsVisible && RenameListGridColumns.TryResolveFieldKey(grid, header) is not null
                    )
                    .OrderBy(header => header.Bounds.Left),
            ];
        }
    }
}
