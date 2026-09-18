using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Pdf
{
    /// <summary>
    /// Shared base for PDF Document Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the PDF group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class PdfRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 40,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            PdfRenameListFields.Group,
            PdfRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.Pdf,
            tip
        );

    /// <summary>
    /// One read-only PDF property column backed by <see cref="PdfDocumentInfo"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the PDF group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">PDF property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class PdfPropertyRenameListField(
        string propertyKey,
        string displayName,
        PdfRenameListProperty field,
        int? defaultWidth = 40,
        string? tip = null
    ) : PdfRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the PDF property addressed by this column.
        /// </summary>
        public PdfRenameListProperty Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return PdfRenameListFieldDisplay.Format(meta.Pdf, Field);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftPdf = left.Pdf;
            var rightPdf = right.Pdf;
            return Field switch
            {
                PdfRenameListProperty.PageCount => RenameListFieldSortCompare.Int32(
                    leftPdf?.PageCount ?? 0,
                    rightPdf?.PageCount ?? 0
                ),
                PdfRenameListProperty.Created => RenameListFieldSortCompare.DateTime(
                    leftPdf?.Created?.UtcDateTime ?? default,
                    rightPdf?.Created?.UtcDateTime ?? default
                ),
                PdfRenameListProperty.Modified => RenameListFieldSortCompare.DateTime(
                    leftPdf?.Modified?.UtcDateTime ?? default,
                    rightPdf?.Modified?.UtcDateTime ?? default
                ),
                PdfRenameListProperty.Title
                or PdfRenameListProperty.Author
                or PdfRenameListProperty.Subject
                or PdfRenameListProperty.Keywords
                or PdfRenameListProperty.Creator
                or PdfRenameListProperty.Producer => base.CompareForSort(left, right),
                _ => base.CompareForSort(left, right),
            };
        }
    }

    /// <summary>
    /// PDF Info properties exposed as Rename List columns.
    /// </summary>
    internal enum PdfRenameListProperty
    {
        /// <summary>Info Title.</summary>
        Title,

        /// <summary>Info Author.</summary>
        Author,

        /// <summary>Info Subject.</summary>
        Subject,

        /// <summary>Info Keywords.</summary>
        Keywords,

        /// <summary>Info Creator.</summary>
        Creator,

        /// <summary>Info Producer.</summary>
        Producer,

        /// <summary>Info CreationDate.</summary>
        Created,

        /// <summary>Info ModDate.</summary>
        Modified,

        /// <summary>Page count.</summary>
        PageCount,
    }

    /// <summary>
    /// Formats <see cref="PdfDocumentInfo"/> for Rename List PDF columns.
    /// </summary>
    internal static class PdfRenameListFieldDisplay
    {
        /// <summary>
        /// Formats one PDF property for grid display.
        /// </summary>
        /// <param name="pdf">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(PdfDocumentInfo? pdf, PdfRenameListProperty field)
        {
            if (pdf is null)
            {
                return string.Empty;
            }

            return field switch
            {
                PdfRenameListProperty.Title => RenameListFieldDisplay.FormatOptionalText(pdf.Title),
                PdfRenameListProperty.Author => RenameListFieldDisplay.FormatOptionalText(pdf.Author),
                PdfRenameListProperty.Subject => RenameListFieldDisplay.FormatOptionalText(pdf.Subject),
                PdfRenameListProperty.Keywords => RenameListFieldDisplay.FormatOptionalText(pdf.Keywords),
                PdfRenameListProperty.Creator => RenameListFieldDisplay.FormatOptionalText(pdf.Creator),
                PdfRenameListProperty.Producer => RenameListFieldDisplay.FormatOptionalText(pdf.Producer),
                PdfRenameListProperty.Created => _FormatDate(pdf.Created),
                PdfRenameListProperty.Modified => _FormatDate(pdf.Modified),
                PdfRenameListProperty.PageCount => RenameListFieldDisplay.FormatPositiveInt(pdf.PageCount),
                _ => string.Empty,
            };
        }

        private static string _FormatDate(DateTimeOffset? value)
        {
            return value is { } date ? RenameListFieldDisplay.FormatFileDate(date.LocalDateTime) : string.Empty;
        }
    }
}
