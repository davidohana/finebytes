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
        /// Field reads TagLib embedded tags and/or media properties from disk.
        /// </summary>
        TagLib = 1,

        /// <summary>
        /// Field reads MetadataExtractor image properties and/or EXIF from disk.
        /// </summary>
        ImageProperties = 2,
    }
}
