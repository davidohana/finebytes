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
        /// <param name="isPreview">When <see langword="true"/>, appends the MFR7 preview suffix.</param>
        /// <returns>Header text shown in the grid.</returns>
        public static string GetColumnHeaderText(RenameListField field, bool isPreview)
        {
            ArgumentNullException.ThrowIfNull(field);
            if (!isPreview)
            {
                return field.DisplayName;
            }

            return $"{field.DisplayName} (Preview)";
        }
    }
}
