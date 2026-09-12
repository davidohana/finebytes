using Mfr.Models.Filters;
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
    /// <param name="supportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added (MFR7 <c>ReadWriteApply</c>).
    /// </param>
    /// <param name="writeTarget">
    /// Filter target for Edit as Name List / Manual Override, or <see langword="null"/> when not writable.
    /// </param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class AudioTagRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 160,
        bool supportsPreview = false,
        FilterTarget? writeTarget = null,
        string? tip = null
    )
        : RenameListField(
            AudioTagRenameListFields.Group,
            AudioTagRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            isSortable: true,
            supportsPreview,
            RenameListMetadataRequirement.TagLib,
            writeTarget,
            tip
        );

    /// <summary>
    /// One semantic audio-tag column backed by <see cref="SemanticFields"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Semantic field to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class AudioTagSemanticRenameListField(
        string propertyKey,
        string displayName,
        SemanticAudioField field,
        int? defaultWidth = 160,
        string? tip = null
    )
        : AudioTagRenameListField(
            propertyKey,
            displayName,
            defaultWidth,
            supportsPreview: true,
            writeTarget: new SemanticAudioFieldTarget(field),
            tip: tip
        )
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

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            if (_IsNumericField(Field))
            {
                return RenameListFieldSortCompare.ParsedInt64(Resolve(left), Resolve(right));
            }

            return base.CompareForSort(left, right);
        }

        private static bool _IsNumericField(SemanticAudioField field)
        {
            return field
                is SemanticAudioField.Year
                    or SemanticAudioField.Track
                    or SemanticAudioField.TrackCount
                    or SemanticAudioField.Disc
                    or SemanticAudioField.DiscCount
                    or SemanticAudioField.BeatsPerMinute;
        }
    }

    /// <summary>
    /// First semicolon-delimited segment of a multi-value semantic audio field.
    /// </summary>
    /// <param name="propertyKey">Property key within the Audio Tag group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Semantic field whose first segment is shown.</param>
    /// <param name="tip">Optional tooltip clarifying the first-segment column.</param>
    internal sealed class AudioTagFirstSegmentRenameListField(
        string propertyKey,
        string displayName,
        SemanticAudioField field,
        string? tip = null
    ) : AudioTagRenameListField(propertyKey, displayName, tip: tip)
    {
        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            var joined = SemanticFields.GetSemanticField(meta.AudioTagOverlay, field);
            return RenameListFieldDisplay.FirstDelimitedSegment(joined);
        }
    }
}
