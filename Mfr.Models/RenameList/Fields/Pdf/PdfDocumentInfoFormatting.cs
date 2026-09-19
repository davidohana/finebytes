using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Pdf
{
    /// <summary>
    /// Formats <see cref="PdfDocumentInfo"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class PdfDocumentInfoFormatting
    {
        /// <summary>
        /// Formats one PDF property for display.
        /// </summary>
        /// <param name="pdf">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. Created/Modified use
        /// <see cref="RenameListFieldDisplay.FormatOptionalDateTimeOffset"/>; other arms are identical.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(PdfDocumentInfo? pdf, PdfDocumentField field, PropertyDisplayContext context)
        {
            if (pdf is null)
            {
                return string.Empty;
            }

            return field switch
            {
                PdfDocumentField.Title => RenameListFieldDisplay.FormatOptionalText(pdf.Title),
                PdfDocumentField.Author => RenameListFieldDisplay.FormatOptionalText(pdf.Author),
                PdfDocumentField.Subject => RenameListFieldDisplay.FormatOptionalText(pdf.Subject),
                PdfDocumentField.Keywords => RenameListFieldDisplay.FormatOptionalText(pdf.Keywords),
                PdfDocumentField.Creator => RenameListFieldDisplay.FormatOptionalText(pdf.Creator),
                PdfDocumentField.Producer => RenameListFieldDisplay.FormatOptionalText(pdf.Producer),
                PdfDocumentField.Created => RenameListFieldDisplay.FormatOptionalDateTimeOffset(pdf.Created, context),
                PdfDocumentField.Modified => RenameListFieldDisplay.FormatOptionalDateTimeOffset(pdf.Modified, context),
                PdfDocumentField.PageCount => RenameListFieldDisplay.FormatPositiveInt(pdf.PageCount),
                _ => throw new UnreachableException(),
            };
        }
    }
}
