using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Mp3
{
    /// <summary>
    /// Formats <see cref="Mp3AudioProperties"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class Mp3AudioPropertiesFormatting
    {
        /// <summary>
        /// Formats one MPEG audio property for display.
        /// </summary>
        /// <param name="mp3">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. MP3 arms are currently identical for Token and Grid; the
        /// parameter is required so MP3 shares one formatter signature with Image/PDF/Media.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(
            Mp3AudioProperties? mp3,
            Mp3AudioPropertyField field,
            PropertyDisplayContext context
        )
        {
            // No Token vs Grid fork for MP3 today; keep the parameter for signature parity.
            _ = context;

            if (mp3 is null)
            {
                return string.Empty;
            }

            return field switch
            {
                Mp3AudioPropertyField.Bitrate => _FormatBitrate(mp3),
                Mp3AudioPropertyField.Copyright => RenameListFieldDisplay.FormatYesNo(mp3.IsCopyrighted),
                Mp3AudioPropertyField.Duration => RenameListFieldDisplay.FormatDuration(mp3.Duration),
                Mp3AudioPropertyField.DurationSec => RenameListFieldDisplay.FormatDurationSec(mp3.Duration),
                Mp3AudioPropertyField.Encoding => mp3.IsVbr ? "VBR" : "CBR",
                Mp3AudioPropertyField.Frequency => RenameListFieldDisplay.FormatPositiveInt(mp3.SampleRate),
                Mp3AudioPropertyField.Layer => _FormatLayer(mp3.Layer),
                Mp3AudioPropertyField.Ver => RenameListFieldDisplay.FormatOptionalText(mp3.MpegVersion),
                Mp3AudioPropertyField.Mode => RenameListFieldDisplay.FormatOptionalText(mp3.ChannelMode),
                Mp3AudioPropertyField.Original => RenameListFieldDisplay.FormatYesNo(mp3.IsOriginal),
                Mp3AudioPropertyField.Protection => RenameListFieldDisplay.FormatYesNo(mp3.IsProtected),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Formats bitrate as invariant digits, prefixed with <c>VBR</c> when the stream is VBR.
        /// </summary>
        private static string _FormatBitrate(Mp3AudioProperties mp3)
        {
            var rate = RenameListFieldDisplay.FormatPositiveInt(mp3.Bitrate);
            if (rate.Length == 0)
            {
                return string.Empty;
            }

            if (mp3.IsVbr)
            {
                return "VBR" + rate;
            }

            return rate;
        }

        /// <summary>
        /// Formats MPEG layer as Roman numerals I–III; other values are treated as absent.
        /// </summary>
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
    }
}
