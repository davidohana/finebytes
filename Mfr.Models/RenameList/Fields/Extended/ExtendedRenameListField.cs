namespace Mfr.Models.RenameList.Fields.Extended
{
    /// <summary>
    /// Shared base for MFR7 Extended ("File Properties") Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Extended group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    public abstract class ExtendedRenameListField(string propertyKey, string displayName, int? defaultWidth = null)
        : RenameListField(propertyKey, displayName, defaultWidth, isSortable: false, supportsPreview: false)
    {
        /// <summary>
        /// MFR7 Extended property group id.
        /// </summary>
        public const string Group = "Extended";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "File Properties";

        /// <inheritdoc />
        public sealed override string GroupId => Group;

        /// <inheritdoc />
        public sealed override string GroupDisplayName => GroupLabel;
    }
}
