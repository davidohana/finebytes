namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Format tokens that map to a fixed Rename List catalog field (no arg-dependent mapping).
    /// </summary>
    internal interface IRenameListMappedFormatToken
    {
        /// <summary>
        /// Resolves the fixed catalog group/property for this token when applicable.
        /// </summary>
        /// <param name="groupId">Catalog group id when mapped.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns><see langword="true"/> when this token has a fixed Rename List field mapping.</returns>
        bool TryGetFixedField(out string groupId, out string propertyKey);
    }
}
