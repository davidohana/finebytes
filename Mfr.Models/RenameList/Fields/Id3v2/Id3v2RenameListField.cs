using Mfr.Models.Filters;
using Mfr.Models.Rename;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Models.RenameList.Fields.Id3v2
{
    /// <summary>
    /// Shared base for MP3 ID3v2 Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the ID3v2 group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="supportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added.
    /// </param>
    /// <param name="writeTarget">
    /// Filter target for Edit as Name List / Manual Override, or <see langword="null"/> when not writable.
    /// </param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class Id3v2RenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 160,
        bool supportsPreview = false,
        FilterTarget? writeTarget = null,
        string? tip = null
    )
        : RenameListField(
            Id3v2RenameListFields.Group,
            Id3v2RenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            isSortable: true,
            supportsPreview,
            RenameListMetadataRequirement.TagLib,
            writeTarget,
            tip
        );

    /// <summary>
    /// One modeled ID3v2 frame column backed by <see cref="AudioOverlayBlockFieldIo"/>.
    /// </summary>
    /// <param name="frameId">Four-character modeled frame id (property key).</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class Id3v2FrameRenameListField(string frameId, int? defaultWidth = 160)
        : Id3v2RenameListField(
            frameId,
            Id3v2FrameLabels.For(frameId),
            defaultWidth,
            supportsPreview: true,
            writeTarget: new Id3v2FrameTarget(frameId),
            tip: $"{Id3v2RenameListFields.GroupLabel}: {frameId}."
        )
    {
        /// <summary>
        /// Gets the modeled frame id addressed by this column.
        /// </summary>
        public string FrameId { get; } = frameId;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return AudioOverlayBlockFieldIo.GetId3v2FrameString(meta.AudioTagOverlay, FrameId);
        }
    }

    /// <summary>
    /// Read-only ID3v2 tag version column (MFR7-style <c>2.3</c> / <c>2.4</c>).
    /// </summary>
    internal sealed class Id3v2VersionRenameListField()
        : Id3v2RenameListField(
            Id3v2RenameListFields.Key.Version,
            "Version (ID3v2)",
            defaultWidth: 80,
            tip: $"{Id3v2RenameListFields.GroupLabel}: tag version (e.g. 2.3 / 2.4)."
        )
    {
        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            var block = meta.AudioTagOverlay.Id3v2;
            if (block is null)
            {
                return string.Empty;
            }

            return $"2.{block.Version}";
        }
    }
}
