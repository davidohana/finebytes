namespace Mfr.Models.RenameList.Fields.Media
{
    /// <summary>
    /// All MFR7 Media Properties Rename List fields (read-only originals).
    /// </summary>
    public static class MediaRenameListFields
    {
        /// <summary>
        /// MFR7 Media Properties group id.
        /// </summary>
        public const string Group = "MediaProperties";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "Media Properties";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>MIME type.</summary>
            public const string MimeType = "MimeType";

            /// <summary>Possibly-corrupt flag.</summary>
            public const string PossiblyCorrupt = "PossiblyCorrupt";

            /// <summary>Duration text.</summary>
            public const string Duration = "Duration";

            /// <summary>Duration in seconds.</summary>
            public const string DurationSeconds = "DurationSeconds";

            /// <summary>Media type flags text.</summary>
            public const string MediaTypes = "MediaTypes";

            /// <summary>Codec description.</summary>
            public const string Description = "Description";

            /// <summary>Audio bitrate.</summary>
            public const string AudioBitrate = "AudioBitrate";

            /// <summary>Audio channel count.</summary>
            public const string AudioChannels = "AudioChannels";

            /// <summary>Audio sample rate.</summary>
            public const string AudioSampleRate = "AudioSampleRate";

            /// <summary>Bits per sample.</summary>
            public const string BitsPerSample = "BitsPerSample";

            /// <summary>Video width.</summary>
            public const string VideoWidth = "VideoWidth";

            /// <summary>Video height.</summary>
            public const string VideoHeight = "VideoHeight";

            /// <summary>Photo width.</summary>
            public const string PhotoWidth = "PhotoWidth";

            /// <summary>Photo height.</summary>
            public const string PhotoHeight = "PhotoHeight";

            /// <summary>Photo quality.</summary>
            public const string PhotoQuality = "PhotoQuality";
        }

        /// <summary>
        /// Media Properties group fields in catalog order (identity, duration, audio, video, photo).
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new MediaPropertyRenameListField(Key.MimeType, "MIME Type", MediaPropertyField.MimeType),
            new MediaPropertyRenameListField(
                Key.PossiblyCorrupt,
                "Possibly Corrupt",
                MediaPropertyField.Corrupt,
                defaultWidth: 40,
                tip: MediaRenameListFieldTips.PossiblyCorrupt
            ),
            new MediaPropertyRenameListField(
                Key.Duration,
                "Duration",
                MediaPropertyField.Duration,
                tip: MediaRenameListFieldTips.Duration
            ),
            new MediaPropertyRenameListField(
                Key.DurationSeconds,
                "Duration (Seconds)",
                MediaPropertyField.DurationSec,
                tip: MediaRenameListFieldTips.DurationSeconds
            ),
            new MediaPropertyRenameListField(Key.MediaTypes, "Media Types", MediaPropertyField.MediaTypes),
            new MediaPropertyRenameListField(
                Key.Description,
                "Description",
                MediaPropertyField.Description,
                defaultWidth: 220
            ),
            new MediaPropertyRenameListField(
                Key.AudioBitrate,
                "Audio Bitrate",
                MediaPropertyField.AudioBitrate,
                defaultWidth: 40
            ),
            new MediaPropertyRenameListField(
                Key.AudioChannels,
                "Audio Channels",
                MediaPropertyField.Channels,
                defaultWidth: 40
            ),
            new MediaPropertyRenameListField(Key.AudioSampleRate, "Audio Sample Rate", MediaPropertyField.SampleRate),
            new MediaPropertyRenameListField(Key.BitsPerSample, "Bits Per Sample", MediaPropertyField.BitsPerSample),
            new MediaPropertyRenameListField(
                Key.VideoWidth,
                "Video Width",
                MediaPropertyField.VideoWidth,
                tip: MediaRenameListFieldTips.VideoWidth
            ),
            new MediaPropertyRenameListField(
                Key.VideoHeight,
                "Video Height",
                MediaPropertyField.VideoHeight,
                tip: MediaRenameListFieldTips.VideoHeight
            ),
            new MediaPropertyRenameListField(
                Key.PhotoWidth,
                "Photo Width",
                MediaPropertyField.PhotoWidth,
                tip: MediaRenameListFieldTips.PhotoWidth
            ),
            new MediaPropertyRenameListField(
                Key.PhotoHeight,
                "Photo Height",
                MediaPropertyField.PhotoHeight,
                tip: MediaRenameListFieldTips.PhotoHeight
            ),
            new MediaPropertyRenameListField(
                Key.PhotoQuality,
                "Photo Quality",
                MediaPropertyField.PhotoQuality,
                defaultWidth: 40
            ),
        ];
    }
}
