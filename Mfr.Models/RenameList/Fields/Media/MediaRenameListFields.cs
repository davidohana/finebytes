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
        /// Media Properties group fields in catalog order (identity, duration, audio, video, photo).
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new MediaPropertyRenameListField("MimeType", "MIME Type", MediaRenameListProperty.MimeType),
            new MediaPropertyRenameListField(
                "PossiblyCorrupt",
                "Possibly Corrupt",
                MediaRenameListProperty.PossiblyCorrupt,
                defaultWidth: 40
            ),
            new MediaPropertyRenameListField("Duration", "Duration", MediaRenameListProperty.Duration),
            new MediaPropertyRenameListField(
                "DurationSeconds",
                "Duration (Seconds)",
                MediaRenameListProperty.DurationSeconds
            ),
            new MediaPropertyRenameListField("MediaTypes", "Media Types", MediaRenameListProperty.MediaTypes),
            new MediaPropertyRenameListField(
                "Description",
                "Description",
                MediaRenameListProperty.Description,
                defaultWidth: 180
            ),
            new MediaPropertyRenameListField(
                "AudioBitrate",
                "Audio Bitrate",
                MediaRenameListProperty.AudioBitrate,
                defaultWidth: 40
            ),
            new MediaPropertyRenameListField(
                "AudioChannels",
                "Audio Channels",
                MediaRenameListProperty.AudioChannels,
                defaultWidth: 40
            ),
            new MediaPropertyRenameListField(
                "AudioSampleRate",
                "Audio Sample Rate",
                MediaRenameListProperty.AudioSampleRate
            ),
            new MediaPropertyRenameListField("BitsPerSample", "Bits Per Sample", MediaRenameListProperty.BitsPerSample),
            new MediaPropertyRenameListField("VideoWidth", "Video Width", MediaRenameListProperty.VideoWidth),
            new MediaPropertyRenameListField("VideoHeight", "Video Height", MediaRenameListProperty.VideoHeight),
            new MediaPropertyRenameListField("PhotoWidth", "Photo Width", MediaRenameListProperty.PhotoWidth),
            new MediaPropertyRenameListField("PhotoHeight", "Photo Height", MediaRenameListProperty.PhotoHeight),
            new MediaPropertyRenameListField(
                "PhotoQuality",
                "Photo Quality",
                MediaRenameListProperty.PhotoQuality,
                defaultWidth: 40
            ),
        ];
    }
}
