using System.Text.Json.Serialization;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Identifies one Rename List grid column or sort field: property group, property key, and original vs preview.
    /// </summary>
    /// <param name="GroupId">Property group id (e.g. <c>Basic</c>).</param>
    /// <param name="PropertyKey">Property key within the group (e.g. <c>FullName</c>).</param>
    /// <param name="IsPreview">When <see langword="true"/>, values come from the preview snapshot.</param>
    public readonly record struct RenameListFieldKey(
        [property: JsonPropertyName("group")] string GroupId,
        [property: JsonPropertyName("property")] string PropertyKey,
        [property: JsonPropertyName("preview")] bool IsPreview = false
    )
    {
        /// <summary>
        /// Builds an original (non-preview) field key.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <param name="propertyKey">Property key within the group.</param>
        /// <returns>Field key with <see cref="IsPreview"/> <see langword="false"/>.</returns>
        public static RenameListFieldKey Original(string groupId, string propertyKey)
        {
            return new RenameListFieldKey(groupId, propertyKey, IsPreview: false);
        }

        /// <summary>
        /// Builds a preview field key for the same group and property.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <param name="propertyKey">Property key within the group.</param>
        /// <returns>Field key with <see cref="IsPreview"/> <see langword="true"/>.</returns>
        public static RenameListFieldKey Preview(string groupId, string propertyKey)
        {
            return new RenameListFieldKey(groupId, propertyKey, IsPreview: true);
        }
    }
}
