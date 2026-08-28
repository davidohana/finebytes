using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-visible labels for Rename List grid columns.
    /// </summary>
    internal static class RenameListFieldDisplay
    {
        /// <summary>
        /// Gets the grid column header text for a catalog field.
        /// </summary>
        /// <param name="field">Catalog field.</param>
        /// <param name="isPreview">Ignored; preview columns use a badge glyph in the header template.</param>
        /// <returns>Header text shown in the grid.</returns>
        public static string GetColumnHeaderText(RenameListField field, bool isPreview)
        {
            ArgumentNullException.ThrowIfNull(field);
            _ = isPreview;
            return field.DisplayName;
        }
    }
}
