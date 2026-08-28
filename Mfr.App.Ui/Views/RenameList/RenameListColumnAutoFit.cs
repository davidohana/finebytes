using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.GridColumnSizing;
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
        internal const double HeaderResizeHitWidth = DataGridColumnAutoFit.HeaderResizeHitWidth;

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
            fieldKey = default;
            var column = DataGridColumnAutoFit.TryResolveTargetColumn(header, grid, positionRelativeToHeader);
            if (column is null)
            {
                return false;
            }

            var key = RenameListGridColumns.GetFieldKey(column);
            if (key is null)
            {
                return false;
            }

            fieldKey = key.Value;
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
    }
}
