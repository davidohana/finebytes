using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Mfr.App.Ui.Views.GridColumnSizing
{
    /// <summary>
    /// Double-click column-header splitter auto-fit shared by File List report view and Rename List.
    /// </summary>
    internal static class DataGridColumnAutoFit
    {
        /// <summary>
        /// Hit-test width at header edges; matches Avalonia <c>DataGridColumnHeader</c> resize regions.
        /// </summary>
        internal const double HeaderResizeHitWidth = 5;

        /// <summary>
        /// Wires header-splitter double-click auto-fit on <paramref name="grid"/>.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        /// <param name="resolveFitWidth">
        /// Returns the pixel width to apply, or <see langword="null"/> to ignore the gesture.
        /// </param>
        internal static void Attach(DataGrid grid, Func<DataGridColumn, int?> resolveFitWidth)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(resolveFitWidth);

            // Tunnel so we can auto-fit before DataGridColumnHeader starts a resize drag.
            grid.AddHandler(
                InputElement.PointerPressedEvent,
                (_, e) => _OnPointerPressed(grid, e, resolveFitWidth),
                RoutingStrategies.Tunnel
            );
        }

        /// <summary>
        /// Resolves the column to auto-fit when a header resize splitter is double-clicked.
        /// </summary>
        /// <param name="header">Header that received the double-click.</param>
        /// <param name="grid">Owning grid.</param>
        /// <param name="positionRelativeToHeader">Pointer position relative to <paramref name="header"/>.</param>
        /// <returns>The column to the left of the splitter, or <see langword="null"/>.</returns>
        internal static DataGridColumn? TryResolveTargetColumn(
            DataGridColumnHeader header,
            DataGrid grid,
            Point positionRelativeToHeader
        )
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(grid);

            if (!grid.CanUserResizeColumns)
            {
                return null;
            }

            var headerColumn = _TryGetOwningColumn(header, grid);
            if (headerColumn is null)
            {
                return null;
            }

            var headerWidth = header.Bounds.Width;
            if (headerWidth <= 0)
            {
                return null;
            }

            var distanceFromLeft = positionRelativeToHeader.X;
            var distanceFromRight = headerWidth - distanceFromLeft;
            if (distanceFromRight <= HeaderResizeHitWidth)
            {
                return headerColumn;
            }

            if (distanceFromLeft <= HeaderResizeHitWidth)
            {
                return _GetPreviousVisibleColumn(grid, headerColumn);
            }

            return null;
        }

        private static void _OnPointerPressed(
            DataGrid grid,
            PointerPressedEventArgs e,
            Func<DataGridColumn, int?> resolveFitWidth
        )
        {
            if (e.Handled || e.ClickCount != 2)
            {
                return;
            }

            if (e.Source is not Visual source)
            {
                return;
            }

            var header = source.FindAncestorOfType<DataGridColumnHeader>() ?? source as DataGridColumnHeader;
            if (header is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(header).Properties.IsLeftButtonPressed)
            {
                return;
            }

            var column = TryResolveTargetColumn(header, grid, e.GetPosition(header));
            if (column is null)
            {
                return;
            }

            var fitWidth = resolveFitWidth(column);
            if (fitWidth is null)
            {
                return;
            }

            e.Handled = true;
            column.Width = new DataGridLength(fitWidth.Value, DataGridLengthUnitType.Pixel);
        }

        private static DataGridColumn? _TryGetOwningColumn(DataGridColumnHeader header, DataGrid grid)
        {
            var presenter = header.FindAncestorOfType<DataGridColumnHeadersPresenter>();
            if (presenter is null)
            {
                return null;
            }

            var headers = presenter
                .GetVisualChildren()
                .OfType<DataGridColumnHeader>()
                .Where(item => item.IsVisible)
                .OrderBy(item => item.Bounds.Left)
                .ToList();
            var headerIndex = headers.IndexOf(header);
            if (headerIndex < 0)
            {
                return null;
            }

            var columns = grid
                .Columns.Where(column => column.IsVisible)
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            if (headerIndex >= columns.Count)
            {
                return null;
            }

            return columns[headerIndex];
        }

        private static DataGridColumn? _GetPreviousVisibleColumn(DataGrid grid, DataGridColumn column)
        {
            var targetDisplayIndex = column.DisplayIndex - 1;
            if (targetDisplayIndex < 0)
            {
                return null;
            }

            foreach (var candidate in grid.Columns)
            {
                if (candidate.IsVisible && candidate.DisplayIndex == targetDisplayIndex)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
