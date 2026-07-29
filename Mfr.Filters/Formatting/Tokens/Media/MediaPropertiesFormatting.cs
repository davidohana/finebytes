using System.Globalization;
using Mfr.Models;

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
                MediaPropertyField.Corrupt => media.PossiblyCorrupt ? "True" : "False",
                MediaPropertyField.Duration => _FormatDuration(media.Duration),
                MediaPropertyField.DurationSec => _FormatDurationSec(media.Duration),
                MediaPropertyField.MediaTypes => media.MediaTypes ?? string.Empty,
                MediaPropertyField.Description => media.Description ?? string.Empty,
                MediaPropertyField.AudioBitrate => _FormatPositiveInt(media.AudioBitrate),
                MediaPropertyField.SampleRate => _FormatPositiveInt(media.AudioSampleRate),
                MediaPropertyField.BitsPerSample => _FormatPositiveInt(media.BitsPerSample),
                MediaPropertyField.Channels => _FormatPositiveInt(media.AudioChannels),
                MediaPropertyField.VideoWidth => _FormatPositiveInt(media.VideoWidth),
                MediaPropertyField.VideoHeight => _FormatPositiveInt(media.VideoHeight),
                MediaPropertyField.PhotoWidth => _FormatPositiveInt(media.PhotoWidth),
                MediaPropertyField.PhotoHeight => _FormatPositiveInt(media.PhotoHeight),
                MediaPropertyField.PhotoQuality => _FormatPositiveInt(media.PhotoQuality),
                _ => string.Empty,
            };
        }

        private static string _FormatDuration(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return string.Empty;

            var totalHours = (int)duration.TotalHours;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{totalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}");
        }

        private static string _FormatDurationSec(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return string.Empty;

            var seconds = (long)Math.Floor(duration.TotalSeconds);
            return seconds.ToString(CultureInfo.InvariantCulture);
        }

        private static string _FormatPositiveInt(int value)
        {
            if (value == 0)
                return string.Empty;

            return value.ToString(CultureInfo.InvariantCulture);
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
