using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Formats <see cref="MpegAudioProperties"/> fields for formatter tokens.
    /// </summary>
    internal static class MpegAudioPropertiesFormatting
    {
        /// <summary>
        /// Formats an MPEG audio property field for token expansion.
        /// </summary>
        /// <param name="mpeg">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which field to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        public static string Format(MpegAudioProperties? mpeg, MpegAudioPropertyField field)
        {
            if (mpeg is null)
                return string.Empty;

            return field switch
            {
                MpegAudioPropertyField.Bitrate => _FormatBitrate(mpeg),
                MpegAudioPropertyField.Copyright => _FormatYesNo(mpeg.IsCopyrighted),
                MpegAudioPropertyField.Duration => _FormatDuration(mpeg.Duration),
                MpegAudioPropertyField.DurationSec => _FormatDurationSec(mpeg.Duration),
                MpegAudioPropertyField.Encoding => mpeg.IsVbr ? "VBR" : "CBR",
                MpegAudioPropertyField.Frequency => _FormatPositiveInt(mpeg.SampleRate),
                MpegAudioPropertyField.Layer => _FormatLayer(mpeg.Layer),
                MpegAudioPropertyField.MpegVer => mpeg.MpegVersion,
                MpegAudioPropertyField.Mode => mpeg.ChannelMode,
                MpegAudioPropertyField.Original => _FormatYesNo(mpeg.IsOriginal),
                MpegAudioPropertyField.Protection => _FormatYesNo(mpeg.IsProtected),
                _ => string.Empty,
            };
        }

        private static string _FormatBitrate(MpegAudioProperties mpeg)
        {
            if (mpeg.Bitrate == 0)
                return string.Empty;

            var rate = mpeg.Bitrate.ToString(CultureInfo.InvariantCulture);
            if (mpeg.IsVbr)
                return "VBR" + rate;

            return rate;
        }

        private static string _FormatLayer(int layer)
        {
            return layer switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                _ => string.Empty,
            };
        }

        private static string _FormatYesNo(bool value)
        {
            return value ? "Yes" : "No";
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
    /// Fields exposed by mpeg-* formatter tokens.
    /// </summary>
    internal enum MpegAudioPropertyField
    {
        Bitrate,
        Copyright,
        Duration,
        DurationSec,
        Encoding,
        Frequency,
        Layer,
        MpegVer,
        Mode,
        Original,
        Protection,
    }
}
