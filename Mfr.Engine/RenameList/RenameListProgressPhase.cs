namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Stage within one Rename List background operation (add, metadata, refresh, or preview).
    /// </summary>
    public enum RenameListProgressPhase
    {
        /// <summary>
        /// Walking sources and accepting rename rows.
        /// </summary>
        ResolveSources,

        /// <summary>
        /// Reading TagLib / MetadataExtractor caches, or applying preview filters per row.
        /// </summary>
        LoadMetadata,
    }
}
