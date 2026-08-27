namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Metadata for one selectable Rename List property (original or preview variant).
    /// </summary>
    /// <param name="GroupId">Property group id (e.g. <c>Basic</c>).</param>
    /// <param name="PropertyKey">Property key within the group (e.g. <c>FullName</c>).</param>
    /// <param name="DisplayName">User-visible column label.</param>
    /// <param name="GroupDisplayName">User-visible group label in the field shuttle dropdown.</param>
    /// <param name="DefaultWidth">Default grid column width in pixels (MFR7).</param>
    /// <param name="IsSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="SupportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added (MFR7 non-<c>ReadOnly</c> fields).
    /// </param>
    public sealed record RenameListFieldDefinition(
        string GroupId,
        string PropertyKey,
        string DisplayName,
        string GroupDisplayName,
        int DefaultWidth,
        bool IsSortable,
        bool SupportsPreview
    )
    {
        /// <summary>
        /// Gets the original (non-preview) field key for this definition.
        /// </summary>
        public RenameListFieldKey OriginalKey => RenameListFieldKey.Original(GroupId, PropertyKey);

        /// <summary>
        /// Gets the preview field key for this definition.
        /// </summary>
        public RenameListFieldKey PreviewKey => RenameListFieldKey.Preview(GroupId, PropertyKey);
    }
}
