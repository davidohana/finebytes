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
    internal abstract class AudioTagRenameListField(string propertyKey, string displayName, int? defaultWidth = 100)
        : OriginalOnlyRenameListField(
            AudioTagRenameListFields.Group,
            AudioTagRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListFieldMetadataLoad.EmbeddedAudioTags
        );

    /// <summary>
    /// One semantic audio-tag column backed by <see cref="SemanticFields"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Semantic field to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class AudioTagSemanticRenameListField(
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

    /// <summary>
    /// First semicolon-delimited segment of a multi-value semantic audio field.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Semantic field whose first segment is shown.</param>
    internal sealed class AudioTagFirstSegmentRenameListField(
        string propertyKey,
        string displayName,
        SemanticAudioField field
    ) : AudioTagRenameListField(propertyKey, displayName)
    {
        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            var joined = SemanticFields.GetSemanticField(meta.AudioTagOverlay, field);
            return RenameListFieldDisplay.FirstDelimitedSegment(joined);
        }
    }
}
