namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Shared copy helpers for the Rename List row-error dialog.
    /// </summary>
    internal static class RenameListRowErrorDisplay
    {
        /// <summary>
        /// Builds clipboard text (summary, path, and details).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListRowErrorDialogContent content)
        {
            return string.Join(
                Environment.NewLine,
                content.Summary,
                content.FilePath,
                string.Empty,
                content.DetailsText
            );
        }

        /// <summary>
        /// Joins a user-facing explanation with an optional technical line.
        /// </summary>
        /// <param name="explanation">Plain-language message.</param>
        /// <param name="technicalDetails">Optional exception or reader text.</param>
        /// <returns><paramref name="explanation"/>, plus a following technical line when present.</returns>
        internal static string FormatDetailsBlock(string explanation, string? technicalDetails)
        {
            if (string.IsNullOrWhiteSpace(technicalDetails))
            {
                return explanation;
            }

            return $"{explanation}{Environment.NewLine}{technicalDetails}";
        }
    }
}
