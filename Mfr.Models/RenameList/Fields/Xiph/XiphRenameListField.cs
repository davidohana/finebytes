using Mfr.Models.Filters;
using Mfr.Models.Rename;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Models.RenameList.Fields.Xiph
{
    /// <summary>
    /// One Xiph Rename List column backed by <see cref="AudioOverlayBlockFieldIo"/>.
    /// </summary>
    /// <param name="key">Modeled Xiph comment key (also the property key).</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class XiphRenameListField(string key, int? defaultWidth = 160)
        : RenameListField(
            XiphRenameListFields.Group,
            XiphRenameListFields.GroupLabel,
            key,
            $"{XiphKeyLabels.For(key)} (Xiph)",
            defaultWidth,
            isSortable: true,
            supportsPreview: true,
            RenameListMetadataRequirement.TagLib,
            writeTarget: new XiphFieldTarget(key),
            tip: _Tip(key)
        )
    {
        /// <summary>
        /// Gets the modeled Xiph comment key addressed by this column.
        /// </summary>
        public string Key { get; } = key;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return AudioOverlayBlockFieldIo.GetXiphFieldString(meta.AudioTagOverlay, Key);
        }

        /// <summary>
        /// Builds a column tooltip that always names the Xiph block (and keeps Apply-To tips when set).
        /// </summary>
        private static string _Tip(string key)
        {
            var detail = XiphKeyLabels.Tip(key);
            if (detail is null)
            {
                return $"{XiphRenameListFields.GroupLabel}: {key}.";
            }

            return $"{detail} ({XiphRenameListFields.GroupLabel})";
        }
    }
}
