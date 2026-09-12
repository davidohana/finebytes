namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List Show Rename Error.
    /// </summary>
    internal static class RenameListCommitErrorDisplay
    {
        /// <summary>
        /// Window title for Show Rename Error.
        /// </summary>
        internal const string DialogTitle = "Rename Error";

        /// <summary>
        /// Short summary shown at the top of the dialog.
        /// </summary>
        internal const string Summary = "This item could not be renamed.";

        /// <summary>
        /// Builds shared dialog content for Show Rename Error.
        /// </summary>
        /// <param name="filePath">Absolute path of the errored row.</param>
        /// <param name="message">User-facing commit error message.</param>
        /// <param name="technicalDetails">Optional exception text for the Technical details expander.</param>
        /// <returns>Title, summary, path, user message, and optional technical details.</returns>
        internal static RenameListRowErrorDialogContent Create(
            string filePath,
            string message,
            string? technicalDetails
        )
        {
            return new RenameListRowErrorDialogContent(
                DialogTitle,
                Summary,
                filePath,
                message,
                string.IsNullOrWhiteSpace(technicalDetails) ? null : technicalDetails
            );
        }
    }
}
