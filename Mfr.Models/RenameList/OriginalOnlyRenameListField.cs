namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Catalog field that is original-only: no Auto-Sort mapping and no preview column.
    /// </summary>
    /// <param name="groupId">MFR7 property group id.</param>
    /// <param name="groupDisplayName">User-visible group label in the field shuttle dropdown.</param>
    /// <param name="propertyKey">Property key within the group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="metadataLoad">Lazy disk metadata required before resolving this field.</param>
    internal abstract class OriginalOnlyRenameListField(
        string groupId,
        string groupDisplayName,
        string propertyKey,
        string displayName,
        int? defaultWidth = null,
        RenameListFieldMetadataLoad metadataLoad = RenameListFieldMetadataLoad.None
    )
        : RenameListField(
            groupId,
            groupDisplayName,
            propertyKey,
            displayName,
            defaultWidth,
            isSortable: false,
            supportsPreview: false,
            sortColumn: null,
            metadataLoad
        );
}
