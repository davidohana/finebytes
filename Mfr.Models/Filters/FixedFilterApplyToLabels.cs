using Mfr.Models.Media;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// Shared user-facing Apply-To text for filters that always write one fixed domain.
    /// </summary>
    public static class FixedFilterApplyToLabels
    {
        /// <summary>
        /// Label for filters that mutate embedded audio tags as a whole (setter / remover).
        /// </summary>
        public const string AudioTags = "Audio tags";

        /// <summary>
        /// Display name for an Extended Rename List column (Attributes, Creation Date, …).
        /// </summary>
        /// <param name="propertyKey">Key within <see cref="ExtendedRenameListFields.Group"/>.</param>
        /// <returns>User-facing column title from <see cref="RenameListFieldCatalog"/>.</returns>
        public static string ForExtendedProperty(string propertyKey)
        {
            return RenameListFieldCatalog.GetField(ExtendedRenameListFields.Group, propertyKey).DisplayName;
        }

        /// <summary>
        /// Maps a filesystem timestamp field to the Extended Rename List column display name.
        /// </summary>
        /// <param name="field">Which timestamp the filter writes.</param>
        /// <returns>Column-style label such as <c>Last Write Date</c>.</returns>
        public static string ForTimestampField(TimestampField field)
        {
            return field switch
            {
                TimestampField.Creation => ForExtendedProperty(ExtendedRenameListFields.Key.CreationDate),
                TimestampField.LastWrite => ForExtendedProperty(ExtendedRenameListFields.Key.LastWriteDate),
                TimestampField.LastAccess => ForExtendedProperty(ExtendedRenameListFields.Key.LastAccessDate),
                _ => ForExtendedProperty(ExtendedRenameListFields.Key.LastWriteDate),
            };
        }
    }
}
