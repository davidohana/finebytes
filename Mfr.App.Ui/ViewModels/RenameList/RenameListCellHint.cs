using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Formats Rename List grid cell text for the status-bar hint.
    /// </summary>
    internal static class RenameListCellHint
    {
        /// <summary>
        /// Theme brush key for preview-kind labels in cell hints.
        /// </summary>
        internal const string PreviewKindBrushKey = "StatusHintPreviewBrush";

        /// <summary>
        /// Builds a rich status-bar hint for a grid cell, matching MFR7 original/preview prefixes.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <param name="isPreviewColumn">Whether the column shows preview values.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatParts(string columnHeader, string cellText, bool isPreviewColumn)
        {
            if (isPreviewColumn)
            {
                return StatusHintDisplay.FromRuns(
                    new StatusHintRun("["),
                    new StatusHintRun("Preview") { ForegroundResourceKey = PreviewKindBrushKey },
                    new StatusHintRun(" "),
                    new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                    new StatusHintRun($"] {cellText}")
                );
            }

            return StatusHintDisplay.FromRuns(
                new StatusHintRun("[Original "),
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($"] {cellText}")
            );
        }

        /// <summary>
        /// Builds plain status-bar text for a grid cell (tests and accessibility).
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <param name="isPreviewColumn">Whether the column shows preview values.</param>
        /// <returns>Single-line hint text.</returns>
        public static string FormatPlainText(string columnHeader, string cellText, bool isPreviewColumn)
        {
            return FormatParts(columnHeader, cellText, isPreviewColumn).ToPlainText();
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
