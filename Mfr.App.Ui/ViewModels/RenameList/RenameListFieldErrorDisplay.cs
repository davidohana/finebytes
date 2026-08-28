namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List original field-load errors.
    /// </summary>
    internal static class RenameListFieldErrorDisplay
    {
        /// <summary>
        /// Short summary shown at the top of the error dialog.
        /// </summary>
        internal const string Summary = "Metadata for this file could not be read from disk.";

        /// <summary>
        /// Builds the details box: friendly explanation plus technical line for each reader failure.
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Folded error text for the single details box.</returns>
        internal static string FormatDetailsText(RenameListFieldErrorDialogContent content)
        {
            var blocks = content.Errors.Select(error => $"{error.UserExplanation}{Environment.NewLine}{error.TechnicalDetails}");
            return string.Join($"{Environment.NewLine}{Environment.NewLine}", blocks);
        }

        /// <summary>
        /// Builds clipboard text for Show Field Error (summary, file, and folded errors).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListFieldErrorDialogContent content)
        {
            return string.Join(
                Environment.NewLine,
                Summary,
                content.FilePath,
                string.Empty,
                FormatDetailsText(content)
            );
        }
    }
}
