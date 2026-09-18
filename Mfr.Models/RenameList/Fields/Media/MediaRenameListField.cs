using System.Diagnostics;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Media
{
    /// <summary>
    /// Shared base for MFR7 Media Properties Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Media Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class MediaRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 60,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            MediaRenameListFields.Group,
            MediaRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.TagLib,
            tip
        );

    /// <summary>
    /// One read-only media property column backed by <see cref="MediaProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Media Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Media property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class MediaPropertyRenameListField(
        string propertyKey,
        string displayName,
        MediaPropertyField field,
        int? defaultWidth = 60,
        string? tip = null
    ) : MediaRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the media property addressed by this column.
        /// </summary>
        public MediaPropertyField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="MediaPropertyField"/> to its Rename List catalog property key.
        /// <para>
        /// Explicit name-drift arms: <see cref="MediaPropertyField.Corrupt"/> →
        /// <see cref="MediaRenameListFields.Key.PossiblyCorrupt"/>,
        /// <see cref="MediaPropertyField.DurationSec"/> →
        /// <see cref="MediaRenameListFields.Key.DurationSeconds"/>,
        /// <see cref="MediaPropertyField.SampleRate"/> →
        /// <see cref="MediaRenameListFields.Key.AudioSampleRate"/>,
        /// <see cref="MediaPropertyField.Channels"/> →
        /// <see cref="MediaRenameListFields.Key.AudioChannels"/>.
        /// </para>
        /// </summary>
        /// <param name="field">Media property field.</param>
        /// <returns>Catalog key under <see cref="MediaRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(MediaPropertyField field)
        {
            return field switch
            {
                MediaPropertyField.MimeType => MediaRenameListFields.Key.MimeType,
                MediaPropertyField.Corrupt => MediaRenameListFields.Key.PossiblyCorrupt,
                MediaPropertyField.Duration => MediaRenameListFields.Key.Duration,
                MediaPropertyField.DurationSec => MediaRenameListFields.Key.DurationSeconds,
                MediaPropertyField.MediaTypes => MediaRenameListFields.Key.MediaTypes,
                MediaPropertyField.Description => MediaRenameListFields.Key.Description,
                MediaPropertyField.AudioBitrate => MediaRenameListFields.Key.AudioBitrate,
                MediaPropertyField.SampleRate => MediaRenameListFields.Key.AudioSampleRate,
                MediaPropertyField.BitsPerSample => MediaRenameListFields.Key.BitsPerSample,
                MediaPropertyField.Channels => MediaRenameListFields.Key.AudioChannels,
                MediaPropertyField.VideoWidth => MediaRenameListFields.Key.VideoWidth,
                MediaPropertyField.VideoHeight => MediaRenameListFields.Key.VideoHeight,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return MediaPropertiesFormatting.Format(meta.Media, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftMedia = left.Media;
            var rightMedia = right.Media;
            return Field switch
            {
                MediaPropertyField.Duration or MediaPropertyField.DurationSec => RenameListFieldSortCompare.TimeSpan(
                    leftMedia?.Duration ?? TimeSpan.Zero,
                    rightMedia?.Duration ?? TimeSpan.Zero
                ),
                MediaPropertyField.AudioBitrate => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioBitrate ?? 0,
                    rightMedia?.AudioBitrate ?? 0
                ),
                MediaPropertyField.Channels => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioChannels ?? 0,
                    rightMedia?.AudioChannels ?? 0
                ),
                MediaPropertyField.SampleRate => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioSampleRate ?? 0,
                    rightMedia?.AudioSampleRate ?? 0
                ),
                MediaPropertyField.BitsPerSample => RenameListFieldSortCompare.Int32(
                    leftMedia?.BitsPerSample ?? 0,
                    rightMedia?.BitsPerSample ?? 0
                ),
                MediaPropertyField.VideoWidth => RenameListFieldSortCompare.Int32(
                    leftMedia?.VideoWidth ?? 0,
                    rightMedia?.VideoWidth ?? 0
                ),
                MediaPropertyField.VideoHeight => RenameListFieldSortCompare.Int32(
                    leftMedia?.VideoHeight ?? 0,
                    rightMedia?.VideoHeight ?? 0
                ),
                MediaPropertyField.MimeType
                or MediaPropertyField.Corrupt
                or MediaPropertyField.MediaTypes
                or MediaPropertyField.Description => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
