namespace Mfr8.Models
{
    /// <summary>
    /// Lightweight file entry used during rename planning and execution.
    /// </summary>
    /// <param name="GlobalIndex">Zero-based index across all scanned files.</param>
    /// <param name="FolderOccurrenceIndex">Zero-based index within the parent folder.</param>
    /// <param name="FullPath">Absolute path to the file.</param>
    /// <param name="DirectoryPath">Absolute path to the parent directory.</param>
    /// <param name="Prefix">File name without extension.</param>
    /// <param name="Extension">File extension including the leading dot.</param>
    public sealed record FileEntryLite(
        int GlobalIndex,
        int FolderOccurrenceIndex,
        string FullPath,
        string DirectoryPath,
        string Prefix,
        string Extension);
}
