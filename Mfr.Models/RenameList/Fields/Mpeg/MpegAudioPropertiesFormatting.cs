using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Mpeg
{
    /// <summary>
    /// Formats <see cref="MpegAudioProperties"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class MpegAudioPropertiesFormatting
    {
        /// <summary>
        /// Formats one MPEG audio property for display.
        /// </summary>
        /// <param name="mpeg">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. MPEG arms are currently identical for Token and Grid; the
        /// parameter is required so MPEG shares one formatter signature with Image/PDF/Media.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(
            MpegAudioProperties? mpeg,
            MpegAudioPropertyField field,
            PropertyDisplayContext context
        )
        {
            // No Token vs Grid fork for MPEG today; keep the parameter for signature parity.
            _ = context;

            if (mpeg is null)
            {
                return string.Empty;
            }

            return field switch
            {
                MpegAudioPropertyField.Bitrate => _FormatBitrate(mpeg),
                MpegAudioPropertyField.Copyright => RenameListFieldDisplay.FormatYesNo(mpeg.IsCopyrighted),
                MpegAudioPropertyField.Duration => RenameListFieldDisplay.FormatDuration(mpeg.Duration),
                MpegAudioPropertyField.DurationSec => RenameListFieldDisplay.FormatDurationSec(mpeg.Duration),
                MpegAudioPropertyField.Encoding => mpeg.IsVbr ? "VBR" : "CBR",
                MpegAudioPropertyField.Frequency => RenameListFieldDisplay.FormatPositiveInt(mpeg.SampleRate),
                MpegAudioPropertyField.Layer => _FormatLayer(mpeg.Layer),
                MpegAudioPropertyField.MpegVer => RenameListFieldDisplay.FormatOptionalText(mpeg.MpegVersion),
                MpegAudioPropertyField.Mode => RenameListFieldDisplay.FormatOptionalText(mpeg.ChannelMode),
                MpegAudioPropertyField.Original => RenameListFieldDisplay.FormatYesNo(mpeg.IsOriginal),
                MpegAudioPropertyField.Protection => RenameListFieldDisplay.FormatYesNo(mpeg.IsProtected),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Formats bitrate as invariant digits, prefixed with <c>VBR</c> when the stream is VBR.
        /// </summary>
        private static string _FormatBitrate(MpegAudioProperties mpeg)
        {
            var rate = RenameListFieldDisplay.FormatPositiveInt(mpeg.Bitrate);
            if (rate.Length == 0)
            {
                return string.Empty;
            }

            if (mpeg.IsVbr)
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
