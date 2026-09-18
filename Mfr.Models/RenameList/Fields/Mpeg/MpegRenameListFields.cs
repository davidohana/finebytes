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
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>Bitrate.</summary>
            public const string Bitrate = "Bitrate";

            /// <summary>VBR / encoding flag.</summary>
            public const string VBR = "VBR";

            /// <summary>Sample frequency.</summary>
            public const string Frequency = "Frequency";

            /// <summary>Duration text.</summary>
            public const string Duration = "Duration";

            /// <summary>Duration in seconds.</summary>
            public const string DurationSecs = "DurationSecs";

            /// <summary>MPEG layer.</summary>
            public const string Layer = "Layer";

            /// <summary>MPEG version / level.</summary>
            public const string Level = "Level";

            /// <summary>Channel mode.</summary>
            public const string Mode = "Mode";

            /// <summary>Copyright flag.</summary>
            public const string Copyright = "Copyright";

            /// <summary>Original flag.</summary>
            public const string Original = "Original";

            /// <summary>Protection / CRC flag.</summary>
            public const string Protection = "Protection";
        }

        /// <summary>
        /// MP3 Properties group fields in catalog order (encoding, duration, stream, flags).
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new MpegPropertyRenameListField(Key.Bitrate, "Bitrate", MpegAudioPropertyField.Bitrate, defaultWidth: 40),
            new MpegPropertyRenameListField(Key.VBR, "VBR", MpegAudioPropertyField.Encoding, defaultWidth: 40),
            new MpegPropertyRenameListField(
                Key.Frequency,
                "Frequency",
                MpegAudioPropertyField.Frequency,
                tip: MpegRenameListFieldTips.Frequency
            ),
            new MpegPropertyRenameListField(
                Key.Duration,
                "Duration",
                MpegAudioPropertyField.Duration,
                tip: MpegRenameListFieldTips.Duration
            ),
            new MpegPropertyRenameListField(
                Key.DurationSecs,
                "Duration (Seconds)",
                MpegAudioPropertyField.DurationSec,
                tip: MpegRenameListFieldTips.DurationSecs
            ),
            new MpegPropertyRenameListField(
                Key.Layer,
                "Layer",
                MpegAudioPropertyField.Layer,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Layer
            ),
            new MpegPropertyRenameListField(
                Key.Level,
                "Level",
                MpegAudioPropertyField.MpegVer,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Level
            ),
            new MpegPropertyRenameListField(Key.Mode, "Mode", MpegAudioPropertyField.Mode),
            new MpegPropertyRenameListField(
                Key.Copyright,
                "Copyright",
                MpegAudioPropertyField.Copyright,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Copyright
            ),
            new MpegPropertyRenameListField(
                Key.Original,
                "Original",
                MpegAudioPropertyField.Original,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Original
            ),
            new MpegPropertyRenameListField(
                Key.Protection,
                "Protection",
                MpegAudioPropertyField.Protection,
                defaultWidth: 40,
                tip: MpegRenameListFieldTips.Protection
            ),
        ];
    }
}
