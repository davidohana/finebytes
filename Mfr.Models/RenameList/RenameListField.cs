using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One Rename List catalog field: metadata plus value resolution for original/preview snapshots.
    /// </summary>
    /// <param name="propertyKey">Property key within the group (e.g. <c>FullName</c>).</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="order">Field order within the group (lower first).</param>
    /// <param name="defaultWidth">Default grid column width in pixels (MFR7).</param>
    /// <param name="isSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="supportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added (MFR7 non-<c>ReadOnly</c> fields).
    /// </param>
    /// <param name="sortColumn">Engine Auto-Sort column when this field maps to one.</param>
    /// <param name="isDefaultVisible">When <see langword="true"/>, included in default visible original columns.</param>
    /// <param name="isDefaultVisiblePreview">When <see langword="true"/>, included in default visible preview columns.</param>
    public abstract class RenameListField(
        string propertyKey,
        string displayName,
        int order,
        int defaultWidth = 180,
        bool isSortable = true,
        bool supportsPreview = true,
        RenameListSortColumn? sortColumn = null,
        bool isDefaultVisible = false,
        bool isDefaultVisiblePreview = false
    )
    {
        /// <summary>
        /// Gets the MFR7 property group id.
        /// </summary>
        public abstract string GroupId { get; }

        /// <summary>
        /// Gets the user-visible group label in the field shuttle dropdown.
        /// </summary>
        public abstract string GroupDisplayName { get; }

        /// <summary>
        /// Gets the property key within <see cref="GroupId"/>.
        /// </summary>
        public string PropertyKey { get; } = propertyKey;

        /// <summary>
        /// Gets the user-visible column label.
        /// </summary>
        public string DisplayName { get; } = displayName;

        /// <summary>
        /// Gets shuttle group sort order (lower first).
        /// </summary>
        public virtual int GroupOrder => 0;

        /// <summary>
        /// Gets field order within the group (lower first).
        /// </summary>
        public int Order { get; } = order;

        /// <summary>
        /// Gets default grid column width in pixels (MFR7).
        /// </summary>
        public int DefaultWidth { get; } = defaultWidth;

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
        /// Gets whether this field is a default visible original column.
        /// </summary>
        public bool IsDefaultVisible { get; } = isDefaultVisible;

        /// <summary>
        /// Gets whether this field is a default visible preview column.
        /// </summary>
        public bool IsDefaultVisiblePreview { get; } = isDefaultVisiblePreview;

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
    }
}
