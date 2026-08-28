namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Catalog field that is original-only: sortable but no preview column.
    /// </summary>
    /// <param name="groupId">MFR7 property group id.</param>
    /// <param name="groupDisplayName">User-visible group label in the field shuttle dropdown.</param>
    /// <param name="propertyKey">Property key within the group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="metadataRequirement">Lazy disk metadata required before resolving this field.</param>
    internal abstract class OriginalOnlyRenameListField(
        string groupId,
        string groupDisplayName,
        string propertyKey,
        string displayName,
        int? defaultWidth = null,
        RenameListMetadataRequirement metadataRequirement = RenameListMetadataRequirement.None
    )
        : RenameListField(
            groupId,
            groupDisplayName,
            propertyKey,
            displayName,
            defaultWidth,
            isSortable: true,
            supportsPreview: false,
            metadataRequirement
        );
}
