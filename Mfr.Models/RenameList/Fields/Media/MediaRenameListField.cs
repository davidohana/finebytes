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
    internal abstract class MediaRenameListField(string propertyKey, string displayName, int? defaultWidth = 60)
        : OriginalOnlyRenameListField(
            MediaRenameListFields.Group,
            MediaRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.TagLib
        );

    /// <summary>
    /// One read-only media property column backed by <see cref="MediaProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Media Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Media property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class MediaPropertyRenameListField(
        string propertyKey,
        string displayName,
        MediaRenameListProperty field,
        int? defaultWidth = 60
    ) : MediaRenameListField(propertyKey, displayName, defaultWidth)
    {
        /// <summary>
        /// Gets the media property addressed by this column.
        /// </summary>
        public MediaRenameListProperty Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return MediaRenameListFieldDisplay.Format(meta.Media, Field);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftMedia = left.Media;
            var rightMedia = right.Media;
            return Field switch
            {
                MediaRenameListProperty.Duration or MediaRenameListProperty.DurationSeconds =>
                    RenameListFieldSortCompare.TimeSpan(
                        leftMedia?.Duration ?? TimeSpan.Zero,
                        rightMedia?.Duration ?? TimeSpan.Zero
                    ),
                MediaRenameListProperty.AudioBitrate => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioBitrate ?? 0,
                    rightMedia?.AudioBitrate ?? 0
                ),
                MediaRenameListProperty.AudioChannels => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioChannels ?? 0,
                    rightMedia?.AudioChannels ?? 0
                ),
                MediaRenameListProperty.AudioSampleRate => RenameListFieldSortCompare.Int32(
                    leftMedia?.AudioSampleRate ?? 0,
                    rightMedia?.AudioSampleRate ?? 0
                ),
                MediaRenameListProperty.BitsPerSample => RenameListFieldSortCompare.Int32(
                    leftMedia?.BitsPerSample ?? 0,
                    rightMedia?.BitsPerSample ?? 0
                ),
                MediaRenameListProperty.VideoWidth => RenameListFieldSortCompare.Int32(
                    leftMedia?.VideoWidth ?? 0,
                    rightMedia?.VideoWidth ?? 0
                ),
                MediaRenameListProperty.VideoHeight => RenameListFieldSortCompare.Int32(
                    leftMedia?.VideoHeight ?? 0,
                    rightMedia?.VideoHeight ?? 0
                ),
                MediaRenameListProperty.PhotoWidth => RenameListFieldSortCompare.Int32(
                    leftMedia?.PhotoWidth ?? 0,
                    rightMedia?.PhotoWidth ?? 0
                ),
                MediaRenameListProperty.PhotoHeight => RenameListFieldSortCompare.Int32(
                    leftMedia?.PhotoHeight ?? 0,
                    rightMedia?.PhotoHeight ?? 0
                ),
                MediaRenameListProperty.PhotoQuality => RenameListFieldSortCompare.Int32(
                    leftMedia?.PhotoQuality ?? 0,
                    rightMedia?.PhotoQuality ?? 0
                ),
                MediaRenameListProperty.MimeType
                or MediaRenameListProperty.PossiblyCorrupt
                or MediaRenameListProperty.MediaTypes
                or MediaRenameListProperty.Description => base.CompareForSort(left, right),
                _ => base.CompareForSort(left, right),
            };
        }
    }

    /// <summary>
    /// Media properties exposed as Rename List columns.
    /// </summary>
    internal enum MediaRenameListProperty
    {
        /// <summary>TagLib MIME type.</summary>
        MimeType,

        /// <summary>Whether TagLib marked the file as possibly corrupt.</summary>
        PossiblyCorrupt,

        /// <summary>Media duration.</summary>
        Duration,

        /// <summary>Media duration in whole seconds.</summary>
        DurationSeconds,

        /// <summary>TagLib media-type flags as text.</summary>
        MediaTypes,

        /// <summary>Aggregate codec description.</summary>
        Description,

        /// <summary>Audio bitrate in kbps.</summary>
        AudioBitrate,

        /// <summary>Audio channel count.</summary>
        AudioChannels,

        /// <summary>Audio sample rate in Hz.</summary>
        AudioSampleRate,

        /// <summary>Bits per sample.</summary>
        BitsPerSample,

        /// <summary>Video frame width in pixels.</summary>
        VideoWidth,

        /// <summary>Video frame height in pixels.</summary>
        VideoHeight,

        /// <summary>Photo width in pixels.</summary>
        PhotoWidth,

        /// <summary>Photo height in pixels.</summary>
        PhotoHeight,

        /// <summary>Format-specific photo quality.</summary>
        PhotoQuality,
    }

    /// <summary>
    /// Formats <see cref="MediaProperties"/> for Rename List media columns.
    /// </summary>
    internal static class MediaRenameListFieldDisplay
    {
        /// <summary>
        /// Formats one media property for grid display.
        /// </summary>
        /// <param name="media">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(MediaProperties? media, MediaRenameListProperty field)
        {
            if (media is null)
            {
                return string.Empty;
            }

            return field switch
            {
                MediaRenameListProperty.MimeType => RenameListFieldDisplay.FormatOptionalText(media.MimeType),
                MediaRenameListProperty.PossiblyCorrupt => RenameListFieldDisplay.FormatYesNo(media.PossiblyCorrupt),
                MediaRenameListProperty.Duration => RenameListFieldDisplay.FormatDuration(media.Duration),
                MediaRenameListProperty.DurationSeconds => RenameListFieldDisplay.FormatDurationSec(media.Duration),
                MediaRenameListProperty.MediaTypes => RenameListFieldDisplay.FormatOptionalText(media.MediaTypes),
                MediaRenameListProperty.Description => RenameListFieldDisplay.FormatOptionalText(media.Description),
                MediaRenameListProperty.AudioBitrate => RenameListFieldDisplay.FormatPositiveInt(media.AudioBitrate),
                MediaRenameListProperty.AudioChannels => RenameListFieldDisplay.FormatPositiveInt(media.AudioChannels),
                MediaRenameListProperty.AudioSampleRate => RenameListFieldDisplay.FormatPositiveInt(
                    media.AudioSampleRate
                ),
                MediaRenameListProperty.BitsPerSample => RenameListFieldDisplay.FormatPositiveInt(media.BitsPerSample),
                MediaRenameListProperty.VideoWidth => RenameListFieldDisplay.FormatPositiveInt(media.VideoWidth),
                MediaRenameListProperty.VideoHeight => RenameListFieldDisplay.FormatPositiveInt(media.VideoHeight),
                MediaRenameListProperty.PhotoWidth => RenameListFieldDisplay.FormatPositiveInt(media.PhotoWidth),
                MediaRenameListProperty.PhotoHeight => RenameListFieldDisplay.FormatPositiveInt(media.PhotoHeight),
                MediaRenameListProperty.PhotoQuality => RenameListFieldDisplay.FormatPositiveInt(media.PhotoQuality),
                _ => string.Empty,
            };
        }
    }
}
