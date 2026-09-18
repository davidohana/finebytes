using System.Diagnostics;
using System.Globalization;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Pdf
{
    /// <summary>
    /// Formats <see cref="PdfDocumentInfo"/> fields for formatter tokens.
    /// </summary>
    internal static class PdfDocumentInfoFormatting
    {
        /// <summary>
        /// Formats a PDF Info field for token expansion.
        /// </summary>
        /// <param name="pdf">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which field to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        public static string Format(PdfDocumentInfo? pdf, PdfDocumentField field)
        {
            if (pdf is null)
            {
                return string.Empty;
            }

            return field switch
            {
                PdfDocumentField.Title => _FormatText(pdf.Title),
                PdfDocumentField.Author => _FormatText(pdf.Author),
                PdfDocumentField.Subject => _FormatText(pdf.Subject),
                PdfDocumentField.Keywords => _FormatText(pdf.Keywords),
                PdfDocumentField.Creator => _FormatText(pdf.Creator),
                PdfDocumentField.Producer => _FormatText(pdf.Producer),
                PdfDocumentField.Created => _FormatDate(pdf.Created),
                PdfDocumentField.Modified => _FormatDate(pdf.Modified),
                PdfDocumentField.PageCount => PropertyValueFormatting.PositiveInt(pdf.PageCount),
                _ => throw new UnreachableException(),
            };
        }

        private static string _FormatText(string? value)
        {
            return value.IsBlank() ? string.Empty : value;
        }

        private static string _FormatDate(DateTimeOffset? value)
        {
            return value is { } date ? date.ToString("G", CultureInfo.InvariantCulture) : string.Empty;
        }
    }

    /// <summary>
    /// Fields exposed by <c>pdf-*</c> formatter tokens.
    /// </summary>
    internal enum PdfDocumentField
    {
        Title,
        Author,
        Subject,
        Keywords,
        Creator,
        Producer,
        Created,
        Modified,
        PageCount,
    }
}
