namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Single-flag disk metadata buckets used by Rename List load and <see cref="Rename.RenameItem"/> load state.
    /// </summary>
    internal static class RenameListMetadataBuckets
    {
        /// <summary>
        /// Disk metadata buckets in reader order (TagLib, Image/EXIF, PDF, EPUB).
        /// </summary>
        public static IReadOnlyList<RenameListMetadataRequirement> All { get; } =
        [
            RenameListMetadataRequirement.TagLib,
            RenameListMetadataRequirement.ImageProperties,
            RenameListMetadataRequirement.Pdf,
            RenameListMetadataRequirement.Epub,
        ];

        /// <summary>
        /// Ensures <paramref name="bucket"/> is exactly one entry from <see cref="All"/>.
        /// </summary>
        /// <param name="bucket">Requirement flag to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bucket"/> is <see cref="RenameListMetadataRequirement.None"/>, a combination, or unknown.
        /// </exception>
        public static void RequireSingle(RenameListMetadataRequirement bucket)
        {
            foreach (var known in All)
            {
                if (bucket == known)
                {
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(bucket),
                bucket,
                "Expected a single disk metadata bucket flag."
            );
        }
    }
}
