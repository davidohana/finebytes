namespace Mfr.Models.RenameList.Fields.Mpeg
{
    /// <summary>
    /// All MFR7 MP3 Properties Rename List fields (read-only originals).
    /// </summary>
    public static class MpegRenameListFields
    {
        /// <summary>
        /// MFR7 MP3 property group id.
        /// </summary>
        public const string Group = "MPEG";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "MP3 Properties";

        /// <summary>
        /// MP3 Properties group fields in MFR7 add order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new MpegPropertyRenameListField("Bitrate", "Bitrate", MpegRenameListProperty.Bitrate, defaultWidth: 40),
            new MpegPropertyRenameListField(
                "Copyright",
                "Copyright",
                MpegRenameListProperty.Copyright,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Copyright
            ),
            new MpegPropertyRenameListField(
                "Duration",
                "Duration",
                MpegRenameListProperty.Duration,
                tip: MpegRenameListFieldTips.Duration
            ),
            new MpegPropertyRenameListField(
                "DurationSecs",
                "Duration (Seconds)",
                MpegRenameListProperty.DurationSecs,
                tip: MpegRenameListFieldTips.DurationSecs
            ),
            new MpegPropertyRenameListField("VBR", "VBR", MpegRenameListProperty.Vbr, defaultWidth: 40),
            new MpegPropertyRenameListField(
                "Frequency",
                "Frequency",
                MpegRenameListProperty.Frequency,
                tip: MpegRenameListFieldTips.Frequency
            ),
            new MpegPropertyRenameListField(
                "Layer",
                "Layer",
                MpegRenameListProperty.Layer,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Layer
            ),
            new MpegPropertyRenameListField(
                "Level",
                "Level",
                MpegRenameListProperty.Level,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Level
            ),
            new MpegPropertyRenameListField("Mode", "Mode", MpegRenameListProperty.Mode),
            new MpegPropertyRenameListField(
                "Original",
                "Original",
                MpegRenameListProperty.Original,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Original
            ),
            new MpegPropertyRenameListField(
                "Protection",
                "Protection",
                MpegRenameListProperty.Protection,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Protection
            ),
        ];
    }
}
