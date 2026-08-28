using Avalonia.Controls;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Stores catalog field keys on Rename List grid column tags.
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
    }
}
