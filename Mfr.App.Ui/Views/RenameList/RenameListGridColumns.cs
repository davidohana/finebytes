using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Stores catalog field keys on Rename List grid columns and header content.
    /// </summary>
    internal static class RenameListGridColumns
    {
        /// <summary>
        /// Gets the catalog field key stored on a grid column.
        /// </summary>
        /// <param name="column">Grid column.</param>
        /// <returns>Field key when set; otherwise <see langword="null"/>.</returns>
        public static RenameListFieldKey? GetFieldKey(DataGridColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);
            return column.Tag is RenameListFieldKey key ? key : null;
        }

        /// <summary>
        /// Stores a catalog field key on a grid column.
        /// </summary>
        /// <param name="column">Grid column.</param>
        /// <param name="key">Field key.</param>
        public static void SetFieldKey(DataGridColumn column, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(column);
            column.Tag = key;
        }

        /// <summary>
        /// Stores a catalog field key on header template content.
        /// </summary>
        /// <param name="headerRoot">Header template root control.</param>
        /// <param name="key">Field key.</param>
        public static void StampHeaderFieldKey(Control headerRoot, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(headerRoot);
            headerRoot.Tag = key;
        }

        /// <summary>
        /// Resolves the catalog field key for a column header.
        /// </summary>
        /// <param name="grid">Owning grid.</param>
        /// <param name="header">Clicked or focused header.</param>
        /// <returns>Field key when resolved; otherwise <see langword="null"/>.</returns>
        public static RenameListFieldKey? TryResolveFieldKey(DataGrid grid, DataGridColumnHeader header)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(header);

            var fromHeader = _TryGetFieldKeyFromHeader(header);
            if (fromHeader is not null)
            {
                return fromHeader;
            }

            var column = _TryResolveColumn(grid, header);
            return column is null ? null : GetFieldKey(column);
        }

        private static RenameListFieldKey? _TryGetFieldKeyFromHeader(DataGridColumnHeader header)
        {
            if (header.Tag is RenameListFieldKey headerKey)
            {
                return headerKey;
            }

            if (header.Content is Control content)
            {
                var fromContent = _TryGetFieldKeyFromControl(content);
                if (fromContent is not null)
                {
                    return fromContent;
                }
            }

            foreach (var control in header.GetVisualDescendants().OfType<Control>())
            {
                var key = _TryGetFieldKeyFromControl(control);
                if (key is not null)
                {
                    return key;
                }
            }

            return null;
        }

        private static RenameListFieldKey? _TryGetFieldKeyFromControl(Control control)
        {
            return control.Tag is RenameListFieldKey key ? key : null;
        }

        private static DataGridColumn? _TryResolveColumn(DataGrid grid, DataGridColumnHeader header)
        {
            var presenter = grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>().FirstOrDefault();
            if (presenter is null)
            {
                return null;
            }

            var orderedHeaders = presenter
                .GetVisualChildren()
                .OfType<DataGridColumnHeader>()
                .Where(columnHeader => columnHeader.IsVisible)
                .OrderBy(columnHeader => columnHeader.Bounds.Left)
                .ToList();

            var orderedColumns = grid
                .Columns.Where(column => column.IsVisible && GetFieldKey(column) is not null)
                .OrderBy(column => column.DisplayIndex)
                .ToList();

            var index = orderedHeaders.IndexOf(header);
            if (index < 0 || index >= orderedColumns.Count)
            {
                return null;
            }

            return orderedColumns[index];
        }
    }
}
