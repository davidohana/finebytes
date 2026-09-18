using System.Diagnostics;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Mp3
{
    /// <summary>
    /// Shared base for MFR7 MP3 Properties Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the MP3 Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class Mp3RenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 60,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            Mp3RenameListFields.Group,
            Mp3RenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.TagLib,
            tip
        );

    /// <summary>
    /// One read-only MPEG audio-header column backed by <see cref="Mp3AudioProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the MP3 Properties group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">MPEG property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class Mp3PropertyRenameListField(
        string propertyKey,
        string displayName,
        Mp3AudioPropertyField field,
        int? defaultWidth = 60,
        string? tip = null
    ) : Mp3RenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the MPEG property addressed by this column.
        /// </summary>
        public Mp3AudioPropertyField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="Mp3AudioPropertyField"/> to its Rename List catalog property key.
        /// <para>
        /// Explicit name-drift arms: <see cref="Mp3AudioPropertyField.Encoding"/> →
        /// <see cref="Mp3RenameListFields.Key.VBR"/>,
        /// <see cref="Mp3AudioPropertyField.Ver"/> →
        /// <see cref="Mp3RenameListFields.Key.Level"/>,
        /// <see cref="Mp3AudioPropertyField.DurationSec"/> →
        /// <see cref="Mp3RenameListFields.Key.DurationSecs"/>.
        /// </para>
        /// </summary>
        /// <param name="field">MPEG property field.</param>
        /// <returns>Catalog key under <see cref="Mp3RenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(Mp3AudioPropertyField field)
        {
            return field switch
            {
                Mp3AudioPropertyField.Bitrate => Mp3RenameListFields.Key.Bitrate,
                Mp3AudioPropertyField.Copyright => Mp3RenameListFields.Key.Copyright,
                Mp3AudioPropertyField.Duration => Mp3RenameListFields.Key.Duration,
                Mp3AudioPropertyField.DurationSec => Mp3RenameListFields.Key.DurationSecs,
                Mp3AudioPropertyField.Encoding => Mp3RenameListFields.Key.VBR,
                Mp3AudioPropertyField.Frequency => Mp3RenameListFields.Key.Frequency,
                Mp3AudioPropertyField.Layer => Mp3RenameListFields.Key.Layer,
                Mp3AudioPropertyField.Ver => Mp3RenameListFields.Key.Level,
                Mp3AudioPropertyField.Mode => Mp3RenameListFields.Key.Mode,
                Mp3AudioPropertyField.Original => Mp3RenameListFields.Key.Original,
                Mp3AudioPropertyField.Protection => Mp3RenameListFields.Key.Protection,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return Mp3AudioPropertiesFormatting.Format(meta.Media?.Mp3, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftMp3 = left.Media?.Mp3;
            var rightMp3 = right.Media?.Mp3;
            return Field switch
            {
                Mp3AudioPropertyField.Bitrate => RenameListFieldSortCompare.Int32(
                    leftMp3?.Bitrate ?? 0,
                    rightMp3?.Bitrate ?? 0
                ),
                Mp3AudioPropertyField.Frequency => RenameListFieldSortCompare.Int32(
                    leftMp3?.SampleRate ?? 0,
                    rightMp3?.SampleRate ?? 0
                ),
                Mp3AudioPropertyField.Duration or Mp3AudioPropertyField.DurationSec =>
                    RenameListFieldSortCompare.TimeSpan(
                        leftMp3?.Duration ?? TimeSpan.Zero,
                        rightMp3?.Duration ?? TimeSpan.Zero
                    ),
                Mp3AudioPropertyField.Layer => RenameListFieldSortCompare.Int32(
                    leftMp3?.Layer ?? 0,
                    rightMp3?.Layer ?? 0
                ),
                Mp3AudioPropertyField.Encoding
                or Mp3AudioPropertyField.Ver
                or Mp3AudioPropertyField.Mode
                or Mp3AudioPropertyField.Copyright
                or Mp3AudioPropertyField.Original
                or Mp3AudioPropertyField.Protection => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
