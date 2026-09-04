using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List Show Load Errors (missing path and metadata reader failures).
    /// </summary>
    internal static class RenameListLoadErrorDisplay
    {
        /// <summary>
        /// Window title for Show Load Errors.
        /// </summary>
        internal const string DialogTitle = "Error";

        /// <summary>
        /// Short summary when TagLib or image metadata could not be read.
        /// </summary>
        internal const string MetadataSummary = "Metadata for this file could not be read from disk.";

        /// <summary>
        /// Short summary when the row path no longer exists on disk.
        /// </summary>
        internal const string MissingSummary = "This file or folder is missing from disk.";

        /// <summary>
        /// Builds shared dialog content for Show Load Errors.
        /// </summary>
        /// <param name="filePath">Original absolute path for the selected row.</param>
        /// <param name="errors">Distinct TagLib, image, and/or missing-path failures.</param>
        /// <returns>Title, summary, path, user message, and optional technical details.</returns>
        internal static RenameListRowErrorDialogContent Create(
            string filePath,
            IReadOnlyList<RenameListLoadError> errors
        )
        {
            return new RenameListRowErrorDialogContent(
                DialogTitle,
                FormatSummary(errors),
                filePath,
                FormatUserMessage(errors),
                FormatTechnicalDetails(errors)
            );
        }

        /// <summary>
        /// Short summary shown at the top of the error dialog.
        /// </summary>
        /// <param name="errors">Load issues for the row.</param>
        /// <returns>Missing-path or metadata headline.</returns>
        internal static string FormatSummary(IReadOnlyList<RenameListLoadError> errors)
        {
            return _IsMissingOnly(errors) ? MissingSummary : MetadataSummary;
        }

        /// <summary>
        /// Builds the plain-language message shown in the initial dialog view.
        /// </summary>
        /// <param name="errors">Load issues for the row.</param>
        /// <returns>Joined user explanations.</returns>
        internal static string FormatUserMessage(IReadOnlyList<RenameListLoadError> errors)
        {
            return string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                errors.Select(error => error.UserExplanation)
            );
        }

        /// <summary>
        /// Builds optional technical text for the Technical details expander.
        /// </summary>
        /// <param name="errors">Load issues for the row.</param>
        /// <returns>Joined reader details, or <see langword="null"/> when none apply.</returns>
        internal static string? FormatTechnicalDetails(IReadOnlyList<RenameListLoadError> errors)
        {
            var technicalLines = errors
                .Where(error => !error.IsMissingFromDisk)
                .Select(error => error.TechnicalDetails)
                .Where(details => !string.IsNullOrWhiteSpace(details))
                .ToList();
            if (technicalLines.Count == 0)
            {
                return null;
            }

            return string.Join($"{Environment.NewLine}{Environment.NewLine}", technicalLines);
        }

        private static bool _IsMissingOnly(IReadOnlyList<RenameListLoadError> errors)
        {
            return errors is [{ IsMissingFromDisk: true }];
        }
    }
}
