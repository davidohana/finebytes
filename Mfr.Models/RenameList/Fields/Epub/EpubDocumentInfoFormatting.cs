using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Epub
{
    /// <summary>
    /// Formats <see cref="EpubDocumentInfo"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class EpubDocumentInfoFormatting
    {
        /// <summary>
        /// Formats one EPUB property for display.
        /// </summary>
        /// <param name="epub">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. All EPUB arms use optional-text formatting for both Token and Grid
        /// (Date stays a literal string — no PDF-style date fork); the parameter is kept for
        /// signature parity with PDF/Image/Media formatters.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(EpubDocumentInfo? epub, EpubDocumentField field, PropertyDisplayContext context)
        {
            // No Token vs Grid fork for EPUB today; keep the parameter for signature parity.
            _ = context;

            if (epub is null)
            {
                return string.Empty;
            }

            return field switch
            {
                EpubDocumentField.Title => RenameListFieldDisplay.FormatOptionalText(epub.Title),
                EpubDocumentField.Creator => RenameListFieldDisplay.FormatOptionalText(epub.Creator),
                EpubDocumentField.Publisher => RenameListFieldDisplay.FormatOptionalText(epub.Publisher),
                EpubDocumentField.Language => RenameListFieldDisplay.FormatOptionalText(epub.Language),
                EpubDocumentField.Date => RenameListFieldDisplay.FormatOptionalText(epub.Date),
                EpubDocumentField.Identifier => RenameListFieldDisplay.FormatOptionalText(epub.Identifier),
                EpubDocumentField.Subject => RenameListFieldDisplay.FormatOptionalText(epub.Subject),
                EpubDocumentField.Description => RenameListFieldDisplay.FormatOptionalText(epub.Description),
                _ => throw new UnreachableException(),
            };
        }
    }
}
