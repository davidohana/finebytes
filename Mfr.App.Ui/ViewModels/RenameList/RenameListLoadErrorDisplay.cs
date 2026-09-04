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
        /// <returns>Title, summary, path, and folded details.</returns>
        internal static RenameListRowErrorDialogContent Create(
            string filePath,
            IReadOnlyList<RenameListLoadError> errors
        )
        {
            return new RenameListRowErrorDialogContent(
                DialogTitle,
                FormatSummary(errors),
                filePath,
                FormatDetailsText(errors)
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
        /// Builds the details box: friendly explanation plus technical line for each reader failure.
        /// </summary>
        /// <param name="errors">Load issues for the row.</param>
        /// <returns>Folded error text for the single details box.</returns>
        internal static string FormatDetailsText(IReadOnlyList<RenameListLoadError> errors)
        {
            var blocks = errors.Select(_FormatDetailsBlock);
            return string.Join($"{Environment.NewLine}{Environment.NewLine}", blocks);
        }

        private static string _FormatDetailsBlock(RenameListLoadError error)
        {
            if (error.IsMissingFromDisk)
            {
                return error.UserExplanation;
            }

            return RenameListRowErrorDisplay.FormatDetailsBlock(error.UserExplanation, error.TechnicalDetails);
        }

        private static bool _IsMissingOnly(IReadOnlyList<RenameListLoadError> errors)
        {
            return errors is [{ IsMissingFromDisk: true }];
        }
    }
}
