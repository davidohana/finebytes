using Mfr.Models.Filters;
using Mfr.Models.Rename;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Models.RenameList.Fields.Id3v1
{
    /// <summary>
    /// One ID3v1 Rename List column backed by <see cref="AudioOverlayBlockFieldIo"/>.
    /// </summary>
    /// <param name="field">Which ID3v1 scalar this column addresses (also the property key).</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class Id3v1RenameListField(Id3v1Field field, int? defaultWidth = 160)
        : RenameListField(
            Id3v1RenameListFields.Group,
            Id3v1RenameListFields.GroupLabel,
            field.ToString(),
            $"{field} (ID3v1)",
            defaultWidth,
            isSortable: true,
            supportsPreview: true,
            RenameListMetadataRequirement.TagLib,
            writeTarget: new Id3v1FieldTarget(field),
            tip: $"{Id3v1FieldTips.For(field)} ({Id3v1RenameListFields.GroupLabel})"
        )
    {
        /// <summary>
        /// Gets the ID3v1 scalar addressed by this column.
        /// </summary>
        public Id3v1Field Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return AudioOverlayBlockFieldIo.GetId3v1FieldString(meta.AudioTagOverlay, Field);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            if (Field is Id3v1Field.Year or Id3v1Field.Track)
            {
                return RenameListFieldSortCompare.ParsedInt64(Resolve(left), Resolve(right));
            }

            return base.CompareForSort(left, right);
        }
    }
}
