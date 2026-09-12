using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Formats Rename List grid cell text for the status-bar hint.
    /// </summary>
    internal static class RenameListCellHint
    {
        /// <summary>
        /// MFR7 status-bar marker prepended to the cell value when the row has a preview failure.
        /// </summary>
        public const string PreviewErrorMarker = "[Item Preview Error]";

        /// <summary>
        /// Builds a rich status-bar hint: bold column name, colon, then the cell value.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StyledTextDisplay FormatParts(string columnHeader, string cellText)
        {
            return StyledTextDisplay.FromRuns(
                new StyledTextRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StyledTextRun($": {cellText}")
            );
        }

        /// <summary>
        /// Builds a status-bar hint for a preview-error row: bold column, error-colored marker, then value.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="cellText">Cell display value (without the preview-error marker).</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        public static StyledTextDisplay FormatPreviewError(string columnHeader, string cellText)
        {
            return StyledTextDisplay.FromRuns(
                new StyledTextRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StyledTextRun(": "),
                new StyledTextRun(PreviewErrorMarker)
                {
                    ForegroundResourceKey = StatusBarText.ErrorForegroundResourceKey,
                },
                new StyledTextRun($" {cellText}")
            );
        }

        /// <summary>
        /// Builds the status-bar hint for a failed original metadata cell.
        /// </summary>
        /// <param name="columnHeader">Grid column header text.</param>
        /// <param name="userExplanation">Plain-language load-failure explanation.</param>
        /// <returns>Hint shown in the main window status bar.</returns>
        /// <remarks>
        /// <para>
        /// Column name stays bold and neutral; the explanation uses the status-bar error brush.
        /// </para>
        /// </remarks>
        public static StyledTextDisplay FormatLoadError(string columnHeader, string userExplanation)
        {
            return StyledTextDisplay.FromRuns(
                new StyledTextRun(columnHeader) { FontWeight = FontWeight.Bold },
                new StyledTextRun(": "),
                new StyledTextRun($"Could not read metadata: {userExplanation}")
                {
                    ForegroundResourceKey = StatusBarText.ErrorForegroundResourceKey,
                }
            );
        }
    }
}
