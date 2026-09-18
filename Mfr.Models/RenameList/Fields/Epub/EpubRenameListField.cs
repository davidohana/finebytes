using System.Diagnostics;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Epub
{
    /// <summary>
    /// Shared base for EPUB Document Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the EPUB group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class EpubRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 40,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            EpubRenameListFields.Group,
            EpubRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.Epub,
            tip
        );

    /// <summary>
    /// One read-only EPUB property column backed by <see cref="EpubDocumentInfo"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the EPUB group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">EPUB property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class EpubPropertyRenameListField(
        string propertyKey,
        string displayName,
        EpubDocumentField field,
        int? defaultWidth = 40,
        string? tip = null
    ) : EpubRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the EPUB property addressed by this column.
        /// </summary>
        public EpubDocumentField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="EpubDocumentField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">EPUB property field.</param>
        /// <returns>Catalog key under <see cref="EpubRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(EpubDocumentField field)
        {
            return field switch
            {
                EpubDocumentField.Title => EpubRenameListFields.Key.Title,
                EpubDocumentField.Creator => EpubRenameListFields.Key.Creator,
                EpubDocumentField.Publisher => EpubRenameListFields.Key.Publisher,
                EpubDocumentField.Language => EpubRenameListFields.Key.Language,
                EpubDocumentField.Date => EpubRenameListFields.Key.Date,
                EpubDocumentField.Identifier => EpubRenameListFields.Key.Identifier,
                EpubDocumentField.Subject => EpubRenameListFields.Key.Subject,
                EpubDocumentField.Description => EpubRenameListFields.Key.Description,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return EpubDocumentInfoFormatting.Format(meta.Epub, Field, PropertyDisplayContext.Grid);
        }
    }
}
