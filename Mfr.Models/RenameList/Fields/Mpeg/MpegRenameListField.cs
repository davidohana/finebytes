using System.Globalization;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Mpeg
{
    /// <summary>
    /// Shared base for MFR7 MP3 Properties Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the MP3 Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class MpegRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 60,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            MpegRenameListFields.Group,
            MpegRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.TagLib,
            tip
        );

    /// <summary>
    /// One read-only MPEG audio-header column backed by <see cref="MpegAudioProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the MP3 Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">MPEG property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class MpegPropertyRenameListField(
        string propertyKey,
        string displayName,
        MpegRenameListProperty field,
        int? defaultWidth = 60,
        string? tip = null
    ) : MpegRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the MPEG property addressed by this column.
        /// </summary>
        public MpegRenameListProperty Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return MpegRenameListFieldDisplay.Format(meta.Media?.Mpeg, Field);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftMpeg = left.Media?.Mpeg;
            var rightMpeg = right.Media?.Mpeg;
            return Field switch
            {
                MpegRenameListProperty.Bitrate => RenameListFieldSortCompare.Int32(
                    leftMpeg?.Bitrate ?? 0,
                    rightMpeg?.Bitrate ?? 0
                ),
                MpegRenameListProperty.Frequency => RenameListFieldSortCompare.Int32(
                    leftMpeg?.SampleRate ?? 0,
                    rightMpeg?.SampleRate ?? 0
                ),
                MpegRenameListProperty.Duration or MpegRenameListProperty.DurationSecs =>
                    RenameListFieldSortCompare.TimeSpan(
                        leftMpeg?.Duration ?? TimeSpan.Zero,
                        rightMpeg?.Duration ?? TimeSpan.Zero
                    ),
                MpegRenameListProperty.Layer => RenameListFieldSortCompare.Int32(
                    leftMpeg?.Layer ?? 0,
                    rightMpeg?.Layer ?? 0
                ),
                MpegRenameListProperty.Vbr
                or MpegRenameListProperty.Level
                or MpegRenameListProperty.Mode
                or MpegRenameListProperty.Copyright
                or MpegRenameListProperty.Original
                or MpegRenameListProperty.Protection => base.CompareForSort(left, right),
                _ => base.CompareForSort(left, right),
            };
        }
    }

    /// <summary>
    /// MPEG audio-header properties exposed as Rename List columns.
    /// </summary>
    internal enum MpegRenameListProperty
    {
        /// <summary>Audio bitrate in kbps.</summary>
        Bitrate,

        /// <summary>VBR vs CBR encoding.</summary>
        Vbr,

        /// <summary>Sample rate in Hz.</summary>
        Frequency,

        /// <summary>Header duration.</summary>
        Duration,

        /// <summary>Duration in whole seconds.</summary>
        DurationSecs,

        /// <summary>MPEG audio layer.</summary>
        Layer,

        /// <summary>MPEG version.</summary>
        Level,

        /// <summary>Channel mode.</summary>
        Mode,

        /// <summary>Copyright bit.</summary>
        Copyright,

        /// <summary>Original bit.</summary>
        Original,

        /// <summary>CRC protection bit.</summary>
        Protection,
    }

    /// <summary>
    /// Formats <see cref="MpegAudioProperties"/> for Rename List MP3 columns.
    /// </summary>
    internal static class MpegRenameListFieldDisplay
    {
        /// <summary>
        /// Formats one MPEG property for grid display.
        /// </summary>
        /// <param name="mpeg">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(MpegAudioProperties? mpeg, MpegRenameListProperty field)
        {
            if (mpeg is null)
            {
                return string.Empty;
            }

            return field switch
            {
                MpegRenameListProperty.Bitrate => _FormatBitrate(mpeg),
                MpegRenameListProperty.Vbr => mpeg.IsVbr ? "VBR" : "CBR",
                MpegRenameListProperty.Frequency => RenameListFieldDisplay.FormatPositiveInt(mpeg.SampleRate),
                MpegRenameListProperty.Duration => RenameListFieldDisplay.FormatDuration(mpeg.Duration),
                MpegRenameListProperty.DurationSecs => RenameListFieldDisplay.FormatDurationSec(mpeg.Duration),
                MpegRenameListProperty.Layer => _FormatLayer(mpeg.Layer),
                MpegRenameListProperty.Level => mpeg.MpegVersion,
                MpegRenameListProperty.Mode => mpeg.ChannelMode,
                MpegRenameListProperty.Copyright => RenameListFieldDisplay.FormatYesNo(mpeg.IsCopyrighted),
                MpegRenameListProperty.Original => RenameListFieldDisplay.FormatYesNo(mpeg.IsOriginal),
                MpegRenameListProperty.Protection => RenameListFieldDisplay.FormatYesNo(mpeg.IsProtected),
                _ => string.Empty,
            };
        }

        private static string _FormatBitrate(MpegAudioProperties mpeg)
        {
            if (mpeg.Bitrate == 0)
            {
                return string.Empty;
            }

            var rate = mpeg.Bitrate.ToString(CultureInfo.InvariantCulture);
            if (mpeg.IsVbr)
            {
                return "VBR" + rate;
            }

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
    }
}
