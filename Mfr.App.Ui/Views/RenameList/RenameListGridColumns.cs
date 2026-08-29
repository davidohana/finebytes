using Avalonia.Controls;
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
        /// Marker tag for the fixed row-status column (no catalog field key).
        /// </summary>
        internal static readonly object RowStatusColumnMarker = new();

        /// <summary>
        /// Gets whether a grid column is the fixed row-status indicator column.
        /// </summary>
        /// <param name="column">Grid column.</param>
        /// <returns><see langword="true"/> for the leading error-glyph column.</returns>
        public static bool IsRowStatusColumn(DataGridColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);
            return ReferenceEquals(column.Tag, RowStatusColumnMarker);
        }

        /// <summary>
        /// Marks a grid column as the fixed row-status indicator column.
        /// </summary>
        /// <param name="column">Grid column.</param>
        public static void MarkAsRowStatusColumn(DataGridColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);
            column.Tag = RowStatusColumnMarker;
        }

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
        /// Gets visible grid columns' field keys in left-to-right display order.
        /// </summary>
        /// <param name="grid">Owning grid.</param>
        /// <returns>Field keys for columns with a stored key, ordered by display index.</returns>
        public static IReadOnlyList<RenameListFieldKey> GetDisplayedFieldKeys(DataGrid grid)
        {
            ArgumentNullException.ThrowIfNull(grid);

            return
            [
                .. grid
                    .Columns.Select(column => (column, key: GetFieldKey(column)))
                    .Where(item => item.column.IsVisible && item.key is not null)
                    .OrderBy(item => item.column.DisplayIndex)
                    .Select(item => item.key!.Value),
            ];
        }

        /// <summary>
        /// Resolves the catalog field key stamped on a column header.
        /// </summary>
        /// <param name="header">Clicked or focused header.</param>
        /// <returns>Field key when resolved; otherwise <see langword="null"/>.</returns>
        public static RenameListFieldKey? TryResolveFieldKey(DataGridColumnHeader header)
        {
            ArgumentNullException.ThrowIfNull(header);

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
    }
}
