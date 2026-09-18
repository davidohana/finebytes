using System.Diagnostics;
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
        MpegAudioPropertyField field,
        int? defaultWidth = 60,
        string? tip = null
    ) : MpegRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the MPEG property addressed by this column.
        /// </summary>
        public MpegAudioPropertyField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="MpegAudioPropertyField"/> to its Rename List catalog property key.
        /// <para>
        /// Explicit name-drift arms: <see cref="MpegAudioPropertyField.Encoding"/> →
        /// <see cref="MpegRenameListFields.Key.VBR"/>,
        /// <see cref="MpegAudioPropertyField.MpegVer"/> →
        /// <see cref="MpegRenameListFields.Key.Level"/>,
        /// <see cref="MpegAudioPropertyField.DurationSec"/> →
        /// <see cref="MpegRenameListFields.Key.DurationSecs"/>.
        /// </para>
        /// </summary>
        /// <param name="field">MPEG property field.</param>
        /// <returns>Catalog key under <see cref="MpegRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(MpegAudioPropertyField field)
        {
            return field switch
            {
                MpegAudioPropertyField.Bitrate => MpegRenameListFields.Key.Bitrate,
                MpegAudioPropertyField.Copyright => MpegRenameListFields.Key.Copyright,
                MpegAudioPropertyField.Duration => MpegRenameListFields.Key.Duration,
                MpegAudioPropertyField.DurationSec => MpegRenameListFields.Key.DurationSecs,
                MpegAudioPropertyField.Encoding => MpegRenameListFields.Key.VBR,
                MpegAudioPropertyField.Frequency => MpegRenameListFields.Key.Frequency,
                MpegAudioPropertyField.Layer => MpegRenameListFields.Key.Layer,
                MpegAudioPropertyField.MpegVer => MpegRenameListFields.Key.Level,
                MpegAudioPropertyField.Mode => MpegRenameListFields.Key.Mode,
                MpegAudioPropertyField.Original => MpegRenameListFields.Key.Original,
                MpegAudioPropertyField.Protection => MpegRenameListFields.Key.Protection,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return MpegAudioPropertiesFormatting.Format(meta.Media?.Mpeg, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftMpeg = left.Media?.Mpeg;
            var rightMpeg = right.Media?.Mpeg;
            return Field switch
            {
                MpegAudioPropertyField.Bitrate => RenameListFieldSortCompare.Int32(
                    leftMpeg?.Bitrate ?? 0,
                    rightMpeg?.Bitrate ?? 0
                ),
                MpegAudioPropertyField.Frequency => RenameListFieldSortCompare.Int32(
                    leftMpeg?.SampleRate ?? 0,
                    rightMpeg?.SampleRate ?? 0
                ),
                MpegAudioPropertyField.Duration or MpegAudioPropertyField.DurationSec =>
                    RenameListFieldSortCompare.TimeSpan(
                        leftMpeg?.Duration ?? TimeSpan.Zero,
                        rightMpeg?.Duration ?? TimeSpan.Zero
                    ),
                MpegAudioPropertyField.Layer => RenameListFieldSortCompare.Int32(
                    leftMpeg?.Layer ?? 0,
                    rightMpeg?.Layer ?? 0
                ),
                MpegAudioPropertyField.Encoding
                or MpegAudioPropertyField.MpegVer
                or MpegAudioPropertyField.Mode
                or MpegAudioPropertyField.Copyright
                or MpegAudioPropertyField.Original
                or MpegAudioPropertyField.Protection => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
