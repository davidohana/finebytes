using System.Diagnostics;
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
        PdfDocumentField field,
        int? defaultWidth = 40,
        string? tip = null
    ) : PdfRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the PDF property addressed by this column.
        /// </summary>
        public PdfDocumentField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="PdfDocumentField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">PDF property field.</param>
        /// <returns>Catalog key under <see cref="PdfRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(PdfDocumentField field)
        {
            return field switch
            {
                PdfDocumentField.Title => PdfRenameListFields.Key.Title,
                PdfDocumentField.Author => PdfRenameListFields.Key.Author,
                PdfDocumentField.Subject => PdfRenameListFields.Key.Subject,
                PdfDocumentField.Keywords => PdfRenameListFields.Key.Keywords,
                PdfDocumentField.Creator => PdfRenameListFields.Key.Creator,
                PdfDocumentField.Producer => PdfRenameListFields.Key.Producer,
                PdfDocumentField.Created => PdfRenameListFields.Key.Created,
                PdfDocumentField.Modified => PdfRenameListFields.Key.Modified,
                PdfDocumentField.PageCount => PdfRenameListFields.Key.PageCount,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return PdfDocumentInfoFormatting.Format(meta.Pdf, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftPdf = left.Pdf;
            var rightPdf = right.Pdf;
            return Field switch
            {
                PdfDocumentField.PageCount => RenameListFieldSortCompare.Int32(
                    leftPdf?.PageCount ?? 0,
                    rightPdf?.PageCount ?? 0
                ),
                PdfDocumentField.Created => RenameListFieldSortCompare.DateTime(
                    leftPdf?.Created?.UtcDateTime ?? default,
                    rightPdf?.Created?.UtcDateTime ?? default
                ),
                PdfDocumentField.Modified => RenameListFieldSortCompare.DateTime(
                    leftPdf?.Modified?.UtcDateTime ?? default,
                    rightPdf?.Modified?.UtcDateTime ?? default
                ),
                PdfDocumentField.Title
                or PdfDocumentField.Author
                or PdfDocumentField.Subject
                or PdfDocumentField.Keywords
                or PdfDocumentField.Creator
                or PdfDocumentField.Producer => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
