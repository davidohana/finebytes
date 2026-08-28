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
        /// Builds the status-bar hint for a failed original metadata cell (MFR7 field-value error).
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="userExplanation">Plain-language load-failure explanation.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StatusHintDisplay FormatFieldError(string columnHeader, string userExplanation)
        {
            return StatusHintDisplay.FromRuns(
                new StatusHintRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StatusHintRun($": [Field value error] {userExplanation}")
            );
        }
    }
}
