namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Formats Rename List grid cell text for the status-bar hint.
    /// </summary>
    internal static class RenameListCellHint
    {
        /// <summary>
        /// Builds status-bar text for a grid cell, matching MFR7 original/preview prefixes.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <param name="isPreviewColumn">Whether the column shows preview values.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static string Format(string columnHeader, string cellText, bool isPreviewColumn)
        {
            var kind = isPreviewColumn ? "Preview" : "Original";
            return $"[{kind} {columnHeader}] {cellText}";
        }

        /// <summary>
        /// Reads the display value for a Rename List column from a grid row.
        /// </summary>
        /// <param name="entry">Rename List row.</param>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <returns>Cell text, or empty when the column is unknown.</returns>
        public static string GetCellText(RenameListEntry entry, string columnHeader)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return columnHeader switch
            {
                "File/Folder" => entry.FileFolder,
                "Parent Folder" => entry.ParentFolder,
                "Full File Name" => entry.FullFileName,
                "Full File Name (Preview)" => entry.FullFileNamePreview,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Gets whether the column header identifies a preview field column.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <returns><see langword="true"/> for preview columns.</returns>
        public static bool IsPreviewColumn(string columnHeader)
        {
            return string.Equals(columnHeader, "Full File Name (Preview)", StringComparison.Ordinal);
        }
    }
}
