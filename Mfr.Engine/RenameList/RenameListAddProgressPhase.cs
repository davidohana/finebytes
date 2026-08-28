namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Stage within one Rename List add or metadata background operation.
    /// </summary>
    public enum RenameListAddProgressPhase
    {
        /// <summary>
        /// Walking sources and accepting rename rows.
        /// </summary>
        ResolveSources,

        /// <summary>
        /// Reading TagLib / MetadataExtractor caches for visible columns or Auto-Sort.
        /// </summary>
        LoadMetadata,
    }
}
