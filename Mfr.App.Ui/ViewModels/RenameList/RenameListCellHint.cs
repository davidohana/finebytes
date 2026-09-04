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
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatParts(string columnHeader, string cellText)
        {
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": {cellText}")
            );
        }

        /// <summary>
        /// Builds a status-bar hint that notes a row-level preview failure before the cell value.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatPartsWithPreviewError(string columnHeader, string cellText)
        {
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": [Item Preview Error] {cellText}")
            );
        }

        /// <summary>
        /// Builds the status-bar hint for a failed original metadata cell.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="userExplanation">Plain-language load-failure explanation.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatLoadError(string columnHeader, string userExplanation)
        {
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": Could not read metadata: {userExplanation}")
            );
        }
    }
}
