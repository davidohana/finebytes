namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List Show Preview Error.
    /// </summary>
    internal static class RenameListPreviewErrorDisplay
    {
        /// <summary>
        /// Window title for Show Preview Error.
        /// </summary>
        internal const string DialogTitle = "Preview Error";

        /// <summary>
        /// Short summary shown at the top of the dialog.
        /// </summary>
        internal const string Summary = "Preview failed for this item. It will be skipped when applying changes.";

        /// <summary>
        /// Builds shared dialog content for Show Preview Error.
        /// </summary>
        /// <param name="filePath">Absolute path of the errored row.</param>
        /// <param name="message">User-facing preview error message.</param>
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
