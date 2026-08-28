using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Detects header-splitter double-clicks and resolves auto-fit widths for Rename List columns.
    /// </summary>
    internal static class RenameListColumnAutoFit
    {
        /// <summary>
        /// Hit-test width at header edges; matches Avalonia <c>DataGridColumnHeader</c> resize regions.
        /// </summary>
        internal const double HeaderResizeHitWidth = 5;

        /// <summary>
        /// Resolves the column to auto-fit when a header resize splitter is double-clicked.
        /// </summary>
        /// <param name="header">Header that received the double-click.</param>
        /// <param name="grid">Owning grid.</param>
        /// <param name="positionRelativeToHeader">Pointer position relative to <paramref name="header"/>.</param>
        /// <param name="fieldKey">Resolved field key for the column to the left of the splitter.</param>
        /// <returns><see langword="true"/> when a resize target was resolved.</returns>
        internal static bool TryResolveAutoFitFieldKey(
            DataGridColumnHeader header,
            DataGrid grid,
            Point positionRelativeToHeader,
            out RenameListFieldKey fieldKey
        )
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(grid);

            fieldKey = default;
            if (!grid.CanUserResizeColumns)
            {
                return false;
            }

            var headerFieldKey = RenameListGridColumns.TryResolveFieldKey(header);
            if (headerFieldKey is null)
            {
                return false;
            }

            var headerColumn = _FindGridColumn(grid, headerFieldKey.Value);
            if (headerColumn is null)
            {
                return false;
            }

            var headerWidth = header.Bounds.Width;
            if (headerWidth <= 0)
            {
                return false;
            }

            var distanceFromLeft = positionRelativeToHeader.X;
            var distanceFromRight = headerWidth - distanceFromLeft;
            DataGridColumn? targetColumn = null;
            if (distanceFromRight <= HeaderResizeHitWidth)
            {
                targetColumn = headerColumn;
            }
            else if (distanceFromLeft <= HeaderResizeHitWidth)
            {
                targetColumn = _GetPreviousVisibleColumn(grid, headerColumn);
            }

            if (targetColumn is null)
            {
                return false;
            }

            var targetFieldKey = RenameListGridColumns.GetFieldKey(targetColumn);
            if (targetFieldKey is null)
            {
                return false;
            }

            fieldKey = targetFieldKey.Value;
            return true;
        }

        /// <summary>
        /// Resolves the auto-fit pixel width for one visible column.
        /// </summary>
        /// <param name="entries">Grid rows.</param>
        /// <param name="fieldKey">Column field key.</param>
        /// <returns>Clamped fit width in pixels.</returns>
        internal static int ResolveAutoFitWidth(IEnumerable<RenameListEntry> entries, RenameListFieldKey fieldKey)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var field = RenameListFieldCatalog.GetField(fieldKey);
            var canUserSort = !fieldKey.IsPreview && field.SortColumn is not null;
            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                field.DisplayName,
                reserveSortGlyph: canUserSort,
                reservePreviewGlyph: fieldKey.IsPreview
            );

            return RenameListGridColumnWidths.GetAutoFitWidth(entries, fieldKey, minHeaderWidth);
        }

        private static DataGridColumn? _FindGridColumn(DataGrid grid, RenameListFieldKey fieldKey)
        {
            foreach (var column in grid.Columns)
            {
                if (RenameListGridColumns.GetFieldKey(column) == fieldKey)
                {
                    return column;
                }
            }

            return null;
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
