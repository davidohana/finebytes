namespace Mfr.Models
{
    /// <summary>
    /// Lightweight file metadata used during rename planning and execution.
    /// </summary>
    /// <param name="globalIndex">Zero-based index across all scanned files.</param>
    /// <param name="inFolderIndex">Zero-based index within the parent folder.</param>
    /// <param name="directoryPath">Absolute path to the parent directory.</param>
    /// <param name="prefix">File name without extension.</param>
    /// <param name="extension">File extension including the leading dot.</param>
    public sealed class FileMeta(
        int globalIndex,
        int inFolderIndex,
        string directoryPath,
        string prefix,
        string extension)
    {
        /// <summary>
        /// Gets or sets the zero-based index across all scanned files.
        /// </summary>
        public int GlobalIndex { get; set; } = globalIndex;

        /// <summary>
        /// Gets or sets the zero-based index within the parent folder.
        /// </summary>
        public int InFolderIndex { get; set; } = inFolderIndex;

        /// <summary>
        /// Gets the absolute file path.
        /// </summary>
        public string FullPath => Path.Combine(DirectoryPath, Prefix + Extension);

        /// <summary>
        /// Gets or sets the absolute parent directory path.
        /// </summary>
        public string DirectoryPath { get; set; } = directoryPath;

        /// <summary>
        /// Gets or sets the file name without extension.
        /// </summary>
        public string Prefix { get; set; } = prefix;

        /// <summary>
        /// Gets or sets the file extension including the leading dot.
        /// </summary>
        public string Extension { get; set; } = extension;

        /// <summary>
        /// Creates a detached copy of this metadata instance.
        /// </summary>
        /// <returns>A cloned metadata instance.</returns>
        public FileMeta Clone()
        {
            return new FileMeta(
                globalIndex: GlobalIndex,
                inFolderIndex: InFolderIndex,
                directoryPath: DirectoryPath,
                prefix: Prefix,
                extension: Extension);
        }
    }
}
