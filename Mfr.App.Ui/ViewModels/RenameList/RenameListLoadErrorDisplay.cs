using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// User-facing copy for Rename List Show Load Errors (missing path and metadata reader failures).
    /// </summary>
    internal static class RenameListLoadErrorDisplay
    {
        /// <summary>
        /// Short summary when TagLib or image metadata could not be read.
        /// </summary>
        internal const string MetadataSummary = "Metadata for this file could not be read from disk.";

        /// <summary>
        /// Short summary when the row path no longer exists on disk.
        /// </summary>
        internal const string MissingSummary = "This file or folder is missing from disk.";

        /// <summary>
        /// Short summary shown at the top of the error dialog.
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Missing-path or metadata headline.</returns>
        internal static string FormatSummary(RenameListLoadErrorsDialogContent content)
        {
            return _IsMissingOnly(content) ? MissingSummary : MetadataSummary;
        }

        /// <summary>
        /// Builds the details box: friendly explanation plus technical line for each reader failure.
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Folded error text for the single details box.</returns>
        internal static string FormatDetailsText(RenameListLoadErrorsDialogContent content)
        {
            var blocks = content.Errors.Select(_FormatDetailsBlock);
            return string.Join($"{Environment.NewLine}{Environment.NewLine}", blocks);
        }

        /// <summary>
        /// Builds clipboard text for Show Load Errors (summary, file, and folded errors).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListLoadErrorsDialogContent content)
        {
            return string.Join(
                Environment.NewLine,
                FormatSummary(content),
                content.FilePath,
                string.Empty,
                FormatDetailsText(content)
            );
        }

        private static string _FormatDetailsBlock(RenameListLoadError error)
        {
            if (error.IsMissingFromDisk)
            {
                return error.UserExplanation;
            }

            return $"{error.UserExplanation}{Environment.NewLine}{error.TechnicalDetails}";
        }

        private static bool _IsMissingOnly(RenameListLoadErrorsDialogContent content)
        {
            return content.Errors is [{ IsMissingFromDisk: true }];
        }
    }
}
