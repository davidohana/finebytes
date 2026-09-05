namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Shared base for MFR7 Basic ("File Name") Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Basic group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">
    /// Optional grid column width override in pixels for fields whose data is typically wider than the header.
    /// </param>
    /// <param name="isSortable">When <see langword="true"/>, the field may appear in Auto-Sort keys.</param>
    /// <param name="supportsPreview">When <see langword="true"/>, a preview column variant may be added.</param>
    public abstract class BasicRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = null,
        bool isSortable = true,
        bool supportsPreview = true
    ) : RenameListField(Group, GroupLabel, propertyKey, displayName, defaultWidth, isSortable, supportsPreview)
    {
        /// <summary>
        /// MFR7 Basic property group id.
        /// </summary>
        public const string Group = "Basic";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "File Name";
    }
}
