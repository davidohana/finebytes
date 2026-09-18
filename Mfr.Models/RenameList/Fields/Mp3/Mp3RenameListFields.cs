namespace Mfr.Models.RenameList.Fields.Mp3
{
    /// <summary>
    /// All MFR7 MP3 Properties Rename List fields (read-only originals).
    /// </summary>
    public static class Mp3RenameListFields
    {
        /// <summary>
        /// MFR7 MP3 property group id.
        /// </summary>
        public const string Group = "MP3";

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
            new Mp3PropertyRenameListField(Key.Bitrate, "Bitrate", Mp3AudioPropertyField.Bitrate, defaultWidth: 40),
            new Mp3PropertyRenameListField(Key.VBR, "VBR", Mp3AudioPropertyField.Encoding, defaultWidth: 40),
            new Mp3PropertyRenameListField(
                Key.Frequency,
                "Frequency",
                Mp3AudioPropertyField.Frequency,
                tip: Mp3RenameListFieldTips.Frequency
            ),
            new Mp3PropertyRenameListField(
                Key.Duration,
                "Duration",
                Mp3AudioPropertyField.Duration,
                tip: Mp3RenameListFieldTips.Duration
            ),
            new Mp3PropertyRenameListField(
                Key.DurationSecs,
                "Duration (Seconds)",
                Mp3AudioPropertyField.DurationSec,
                tip: Mp3RenameListFieldTips.DurationSecs
            ),
            new Mp3PropertyRenameListField(
                Key.Layer,
                "Layer",
                Mp3AudioPropertyField.Layer,
                defaultWidth: 40,
                tip: Mp3RenameListFieldTips.Layer
            ),
            new Mp3PropertyRenameListField(
                Key.Level,
                "Level",
                Mp3AudioPropertyField.Ver,
                defaultWidth: 40,
                tip: Mp3RenameListFieldTips.Level
            ),
            new Mp3PropertyRenameListField(Key.Mode, "Mode", Mp3AudioPropertyField.Mode),
            new Mp3PropertyRenameListField(
                Key.Copyright,
                "Copyright",
                Mp3AudioPropertyField.Copyright,
                defaultWidth: 40,
                tip: Mp3RenameListFieldTips.Copyright
            ),
            new Mp3PropertyRenameListField(
                Key.Original,
                "Original",
                Mp3AudioPropertyField.Original,
                defaultWidth: 40,
                tip: Mp3RenameListFieldTips.Original
            ),
            new Mp3PropertyRenameListField(
                Key.Protection,
                "Protection",
                Mp3AudioPropertyField.Protection,
                defaultWidth: 40,
                tip: Mp3RenameListFieldTips.Protection
            ),
        ];
    }
}
