namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List Show Preview Error.
    /// </summary>
    internal static class RenameListPreviewErrorDisplay
    {
        /// <summary>
        /// Short summary shown at the top of the dialog.
        /// </summary>
        internal const string Summary = "Preview failed for this item. It will be skipped when applying changes.";

        /// <summary>
        /// Builds the details box text (message plus optional technical line).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Folded error text for the details box.</returns>
        internal static string FormatDetailsText(RenameListPreviewErrorDialogContent content)
        {
            if (string.IsNullOrWhiteSpace(content.TechnicalDetails))
            {
                return content.Message;
            }

            return $"{content.Message}{Environment.NewLine}{content.TechnicalDetails}";
        }

        /// <summary>
        /// Builds clipboard text for Show Preview Error.
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListPreviewErrorDialogContent content)
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
