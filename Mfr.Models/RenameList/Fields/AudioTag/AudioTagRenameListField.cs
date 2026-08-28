using Mfr.Models.Rename;
using Mfr.Models.Tags;

namespace Mfr.Models.RenameList.Fields.AudioTag
{
    /// <summary>
    /// Shared base for MFR7 Audio Tag Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    public abstract class AudioTagRenameListField(string propertyKey, string displayName, int? defaultWidth = 100)
        : RenameListField(propertyKey, displayName, defaultWidth, isSortable: false, supportsPreview: false)
    {
        /// <summary>
        /// MFR7 Audio Tag property group id.
        /// </summary>
        public const string Group = "MediaTag";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "Audio Tag";

        /// <inheritdoc />
        public sealed override string GroupId => Group;

        /// <inheritdoc />
        public sealed override string GroupDisplayName => GroupLabel;

        /// <inheritdoc />
        public sealed override RenameListFieldMetadataLoad MetadataLoad =>
            RenameListFieldMetadataLoad.EmbeddedAudioTags;
    }

    /// <summary>
    /// One semantic audio-tag column backed by <see cref="SemanticFields"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Semantic field to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    public sealed class AudioTagSemanticRenameListField(
        string propertyKey,
        string displayName,
        SemanticAudioField field,
        int? defaultWidth = 100
    ) : AudioTagRenameListField(propertyKey, displayName, defaultWidth)
    {
        /// <summary>
        /// Gets the semantic audio field addressed by this column.
        /// </summary>
        public SemanticAudioField Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return SemanticFields.GetSemanticField(meta.AudioTagOverlay, Field);
        }
    }
}
