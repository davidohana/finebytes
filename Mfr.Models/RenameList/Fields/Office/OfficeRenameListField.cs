using System.Diagnostics;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Office
{
    /// <summary>
    /// Shared base for Office Document Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Office group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class OfficeRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 40,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            OfficeRenameListFields.Group,
            OfficeRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.Office,
            tip
        );

    /// <summary>
    /// One read-only Office property column backed by <see cref="OfficeDocumentInfo"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Office group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Office property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class OfficePropertyRenameListField(
        string propertyKey,
        string displayName,
        OfficeDocumentField field,
        int? defaultWidth = 40,
        string? tip = null
    ) : OfficeRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the Office property addressed by this column.
        /// </summary>
        public OfficeDocumentField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="OfficeDocumentField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">Office property field.</param>
        /// <returns>Catalog key under <see cref="OfficeRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(OfficeDocumentField field)
        {
            return field switch
            {
                OfficeDocumentField.Title => OfficeRenameListFields.Key.Title,
                OfficeDocumentField.Author => OfficeRenameListFields.Key.Author,
                OfficeDocumentField.Subject => OfficeRenameListFields.Key.Subject,
                OfficeDocumentField.Keywords => OfficeRenameListFields.Key.Keywords,
                OfficeDocumentField.Category => OfficeRenameListFields.Key.Category,
                OfficeDocumentField.Description => OfficeRenameListFields.Key.Description,
                OfficeDocumentField.LastModifiedBy => OfficeRenameListFields.Key.LastModifiedBy,
                OfficeDocumentField.Created => OfficeRenameListFields.Key.Created,
                OfficeDocumentField.Modified => OfficeRenameListFields.Key.Modified,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return OfficeDocumentInfoFormatting.Format(meta.Office, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftOffice = left.Office;
            var rightOffice = right.Office;
            return Field switch
            {
                OfficeDocumentField.Created => RenameListFieldSortCompare.DateTime(
                    leftOffice?.Created?.UtcDateTime ?? default,
                    rightOffice?.Created?.UtcDateTime ?? default
                ),
                OfficeDocumentField.Modified => RenameListFieldSortCompare.DateTime(
                    leftOffice?.Modified?.UtcDateTime ?? default,
                    rightOffice?.Modified?.UtcDateTime ?? default
                ),
                OfficeDocumentField.Title
                or OfficeDocumentField.Author
                or OfficeDocumentField.Subject
                or OfficeDocumentField.Keywords
                or OfficeDocumentField.Category
                or OfficeDocumentField.Description
                or OfficeDocumentField.LastModifiedBy => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
