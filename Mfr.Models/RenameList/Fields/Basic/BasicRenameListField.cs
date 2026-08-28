namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Shared base for MFR7 Basic ("File Name") Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Basic group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Default grid column width in pixels (MFR7).</param>
    /// <param name="isSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="supportsPreview">When <see langword="true"/>, a preview column variant may be added.</param>
    /// <param name="sortColumn">Engine Auto-Sort column when this field maps to one.</param>
    public abstract class BasicRenameListField(
        string propertyKey,
        string displayName,
        int defaultWidth = 180,
        bool isSortable = true,
        bool supportsPreview = true,
        RenameListSortColumn? sortColumn = null
    ) : RenameListField(propertyKey, displayName, defaultWidth, isSortable, supportsPreview, sortColumn)
    {
        /// <summary>
        /// MFR7 Basic property group id.
        /// </summary>
        public const string Group = "Basic";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "File Name";

        /// <inheritdoc />
        public sealed override string GroupId => Group;

        /// <inheritdoc />
        public sealed override string GroupDisplayName => GroupLabel;
    }
}
