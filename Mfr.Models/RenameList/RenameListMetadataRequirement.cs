namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Disk metadata buckets required before resolving some Rename List catalog fields.
    /// </summary>
    [Flags]
    public enum RenameListMetadataRequirement
    {
        /// <summary>
        /// Field reads only scan-time <see cref="Rename.FileMeta"/> data.
        /// </summary>
        None = 0,

        /// <summary>
        /// Field reads embedded audio tags from disk via TagLib.
        /// </summary>
        EmbeddedAudioTags = 1,

        /// <summary>
        /// Field reads TagLib media and MPEG stream properties from disk.
        /// </summary>
        MediaProperties = 2,

        /// <summary>
        /// Field reads MetadataExtractor image properties and/or EXIF from disk.
        /// </summary>
        ImageProperties = 4,
    }
}
