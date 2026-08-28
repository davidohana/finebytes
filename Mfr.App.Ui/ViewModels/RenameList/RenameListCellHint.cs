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
            _ = isPreviewColumn;
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": {cellText}")
            );
        }

        /// <summary>
        /// Gets the status-bar column label for a grid column (string header or sort member path).
        /// </summary>
        /// <param name="sortMemberPath">Original-field sort member path, when the header is templated.</param>
        /// <param name="headerText">Plain header text for non-sortable columns.</param>
        /// <returns>Column label used in hints, or <see langword="null"/> when unknown.</returns>
        public static string? GetColumnHeader(string? sortMemberPath, string? headerText)
        {
            if (!string.IsNullOrEmpty(headerText))
            {
                return headerText;
            }

            return sortMemberPath switch
            {
                nameof(RenameListEntry.FileFolder) => "File/Folder",
                nameof(RenameListEntry.ParentFolder) => "Parent Folder",
                nameof(RenameListEntry.FullFileName) => "Full File Name",
                _ => null,
            };
        }
    }
}
