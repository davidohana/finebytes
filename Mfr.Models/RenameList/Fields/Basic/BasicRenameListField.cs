namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Shared base for MFR7 Basic ("File Name") Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Basic group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="order">Field order within the group (lower first).</param>
    /// <param name="defaultWidth">Default grid column width in pixels (MFR7).</param>
    /// <param name="isSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="supportsPreview">When <see langword="true"/>, a preview column variant may be added.</param>
    /// <param name="sortColumn">Engine Auto-Sort column when this field maps to one.</param>
    /// <param name="isDefaultVisible">When <see langword="true"/>, included in default visible original columns.</param>
    /// <param name="isDefaultVisiblePreview">When <see langword="true"/>, included in default visible preview columns.</param>
    public abstract class BasicRenameListField(
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
        : RenameListField(
            propertyKey,
            displayName,
            order,
            defaultWidth,
            isSortable,
            supportsPreview,
            sortColumn,
            isDefaultVisible,
            isDefaultVisiblePreview
        )
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

        /// <inheritdoc />
        public sealed override int GroupOrder => 0;
    }
}
