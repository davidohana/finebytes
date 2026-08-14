namespace Mfr.Filters.Formatting.Tokens.Media
{
    /// <summary>
    /// Formats <see cref="MediaProperties"/> fields for formatter tokens.
    /// </summary>
    internal static class MediaPropertiesFormatting
    {
        /// <summary>
        /// Formats a media property field for token expansion.
        /// </summary>
        /// <param name="media">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which field to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        public static string Format(MediaProperties? media, MediaPropertyField field)
        {
            if (media is null)
                return string.Empty;

            return field switch
            {
                MediaPropertyField.MimeType => media.MimeType ?? string.Empty,
                MediaPropertyField.Corrupt => PropertyValueFormatting.YesNo(media.PossiblyCorrupt),
                MediaPropertyField.Duration => PropertyValueFormatting.Duration(media.Duration),
                MediaPropertyField.DurationSec => PropertyValueFormatting.DurationSec(media.Duration),
                MediaPropertyField.MediaTypes => media.MediaTypes ?? string.Empty,
                MediaPropertyField.Description => media.Description ?? string.Empty,
                MediaPropertyField.AudioBitrate => PropertyValueFormatting.PositiveInt(media.AudioBitrate),
                MediaPropertyField.SampleRate => PropertyValueFormatting.PositiveInt(media.AudioSampleRate),
                MediaPropertyField.BitsPerSample => PropertyValueFormatting.PositiveInt(media.BitsPerSample),
                MediaPropertyField.Channels => PropertyValueFormatting.PositiveInt(media.AudioChannels),
                MediaPropertyField.VideoWidth => PropertyValueFormatting.PositiveInt(media.VideoWidth),
                MediaPropertyField.VideoHeight => PropertyValueFormatting.PositiveInt(media.VideoHeight),
                MediaPropertyField.PhotoWidth => PropertyValueFormatting.PositiveInt(media.PhotoWidth),
                MediaPropertyField.PhotoHeight => PropertyValueFormatting.PositiveInt(media.PhotoHeight),
                MediaPropertyField.PhotoQuality => PropertyValueFormatting.PositiveInt(media.PhotoQuality),
                _ => string.Empty,
            };
        }
    }

    /// <summary>
    /// Fields exposed by media-* formatter tokens.
    /// </summary>
    internal enum MediaPropertyField
    {
        MimeType,
        Corrupt,
        Duration,
        DurationSec,
        MediaTypes,
        Description,
        AudioBitrate,
        SampleRate,
        BitsPerSample,
        Channels,
        VideoWidth,
        VideoHeight,
        PhotoWidth,
        PhotoHeight,
        PhotoQuality,
    }
}
