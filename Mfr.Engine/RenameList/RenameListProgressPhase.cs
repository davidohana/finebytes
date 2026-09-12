namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Stage within one Rename List background operation (add, metadata, refresh, preview, or commit).
    /// </summary>
    public enum RenameListProgressPhase
    {
        /// <summary>
        /// Walking sources and accepting rename rows.
        /// </summary>
        ResolveSources,

        /// <summary>
        /// Reading TagLib / MetadataExtractor caches (hydrate, refresh, or add-after-resolve).
        /// </summary>
        LoadMetadata,

        /// <summary>
        /// Applying preview filters per rename-list row.
        /// </summary>
        ApplyPreview,

        /// <summary>
        /// Applying committed changes per rename-list row.
        /// </summary>
        ApplyCommit,
    }
}
