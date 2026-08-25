using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Formats Rename List grid cell text for the status-bar hint.
    /// </summary>
    internal static class RenameListCellHint
    {
        /// <summary>
        /// Builds a rich status-bar hint: bold column name, colon, then the cell value.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <param name="isPreviewColumn">Whether the column shows preview values.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatParts(string columnHeader, string cellText, bool isPreviewColumn)
        {
            var hintColumnHeader = _GetHintColumnHeader(columnHeader, isPreviewColumn);
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(hintColumnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": {cellText}")
            );
        }

        /// <summary>
        /// Strips grid-only preview suffixes so the kind label is not repeated in the column name.
        /// </summary>
        private static string _GetHintColumnHeader(string columnHeader, bool isPreviewColumn)
        {
            if (!isPreviewColumn)
            {
                return columnHeader;
            }

            const string previewSuffix = " (Preview)";
            if (columnHeader.EndsWith(previewSuffix, StringComparison.Ordinal))
            {
                return columnHeader[..^previewSuffix.Length];
            }

            return columnHeader;
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
