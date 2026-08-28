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
        internal const string Summary = "The original value for this field could not be read from disk.";

        /// <summary>
        /// Note shown under the grid explaining gray Error cells (MFR7 field-load highlighting).
        /// </summary>
        internal const string GrayCellNote =
            "Gray Error cells mean metadata for that column could not be loaded for the row. "
            + "Columns in the same reader group (Audio Tag / media, or image/EXIF) share one failure.";

        /// <summary>
        /// Builds clipboard text for Show Field Error (summary, explanation, and raw details).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListFieldErrorDialogContent content)
        {
            return string.Join(
                Environment.NewLine,
                Summary,
                string.Empty,
                $"Field: {content.FieldDisplayName}",
                content.UserExplanation,
                string.Empty,
                "Technical details:",
                content.TechnicalDetails
            );
        }
    }
}
