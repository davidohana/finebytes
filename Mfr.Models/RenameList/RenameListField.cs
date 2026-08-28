using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One Rename List catalog field: metadata plus value resolution for original/preview snapshots.
    /// </summary>
    /// <param name="groupId">MFR7 property group id (e.g. <c>Basic</c>).</param>
    /// <param name="groupDisplayName">User-visible group label in the field shuttle dropdown.</param>
    /// <param name="propertyKey">Property key within the group (e.g. <c>FullName</c>).</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">
    /// Optional grid column width override in pixels for fields whose data is typically wider than the header.
    /// </param>
    /// <param name="isSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="supportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added (MFR7 non-<c>ReadOnly</c> fields).
    /// </param>
    /// <param name="sortColumn">Engine Auto-Sort column when this field maps to one.</param>
    /// <param name="metadataRequirement">Lazy disk metadata required before resolving this field.</param>
    public abstract class RenameListField(
        string groupId,
        string groupDisplayName,
        string propertyKey,
        string displayName,
        int? defaultWidth = null,
        bool isSortable = true,
        bool supportsPreview = true,
        RenameListSortColumn? sortColumn = null,
        RenameListMetadataRequirement metadataRequirement = RenameListMetadataRequirement.None
    )
    {
        /// <summary>
        /// Gets the MFR7 property group id.
        /// </summary>
        public string GroupId { get; } = groupId;

        /// <summary>
        /// Gets the user-visible group label in the field shuttle dropdown.
        /// </summary>
        public string GroupDisplayName { get; } = groupDisplayName;

        /// <summary>
        /// Gets the property key within <see cref="GroupId"/>.
        /// </summary>
        public string PropertyKey { get; } = propertyKey;

        /// <summary>
        /// Gets the user-visible column label.
        /// </summary>
        public string DisplayName { get; } = displayName;

        /// <summary>
        /// Gets optional default grid column width in pixels when data needs more space than the header.
        /// </summary>
        public int? DefaultWidth { get; } = defaultWidth;

        /// <summary>
        /// Gets whether the field may appear in Auto-Sort keys.
        /// </summary>
        public bool IsSortable { get; } = isSortable;

        /// <summary>
        /// Gets whether a preview column variant may be added.
        /// </summary>
        public bool SupportsPreview { get; } = supportsPreview;

        /// <summary>
        /// Gets the engine Auto-Sort column when this field maps to one.
        /// </summary>
        public RenameListSortColumn? SortColumn { get; } = sortColumn;

        /// <summary>
        /// Gets lazy disk metadata that must be loaded before resolving this field.
        /// </summary>
        public RenameListMetadataRequirement MetadataRequirement { get; } = metadataRequirement;

        /// <summary>
        /// Gets the original (non-preview) field key for this field.
        /// </summary>
        public RenameListFieldKey OriginalKey => RenameListFieldKey.Original(GroupId, PropertyKey);

        /// <summary>
        /// Gets the preview field key for this field.
        /// </summary>
        public RenameListFieldKey PreviewKey => RenameListFieldKey.Preview(GroupId, PropertyKey);

        /// <summary>
        /// Returns the display text for this field on a metadata snapshot.
        /// </summary>
        /// <param name="meta">Original or preview metadata.</param>
        /// <returns>Display string for the grid or sort shuttle.</returns>
        public abstract string Resolve(FileMeta meta);

        /// <summary>
        /// Returns the display text for this field on a rename item.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <param name="isPreview">When <see langword="true"/>, values come from the preview snapshot.</param>
        /// <returns>Display string for the grid or sort shuttle.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        public string Resolve(RenameItem item, bool isPreview)
        {
            ArgumentNullException.ThrowIfNull(item);
            var meta = isPreview ? item.Preview : item.Original;
            return Resolve(meta);
        }
    }
}
