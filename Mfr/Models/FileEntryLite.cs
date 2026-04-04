namespace Mfr.Models
{
    /// <summary>
    /// Lightweight file entry used during rename planning and execution.
    /// </summary>
    public sealed class FileEntryLite
    {
        /// <summary>
        /// Initializes a lightweight file entry.
        /// </summary>
        /// <param name="GlobalIndex">Zero-based index across all scanned files.</param>
        /// <param name="InFolderIndex">Zero-based index within the parent folder.</param>
        /// <param name="FullPath">Absolute path to the file.</param>
        /// <param name="DirectoryPath">Absolute path to the parent directory.</param>
        /// <param name="Prefix">File name without extension.</param>
        /// <param name="Extension">File extension including the leading dot.</param>
        public FileEntryLite(
            int GlobalIndex,
            int InFolderIndex,
            string FullPath,
            string DirectoryPath,
            string Prefix,
            string Extension)
        {
            this.GlobalIndex = GlobalIndex;
            this.InFolderIndex = InFolderIndex;
            this.DirectoryPath = DirectoryPath;
            this.Prefix = Prefix;
            this.Extension = Extension;

            var expectedFullPath = Path.Combine(DirectoryPath, Prefix + Extension);
            if (!string.Equals(FullPath, expectedFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("FullPath must match DirectoryPath + Prefix + Extension.", nameof(FullPath));
            }
        }

        /// <summary>
        /// Gets or sets the zero-based index across all scanned files.
        /// </summary>
        public int GlobalIndex { get; set; }

        /// <summary>
        /// Gets or sets the zero-based index within the parent folder.
        /// </summary>
        public int InFolderIndex { get; set; }

        /// <summary>
        /// Gets the absolute file path.
        /// </summary>
        public string FullPath => Path.Combine(DirectoryPath, Prefix + Extension);

        /// <summary>
        /// Gets or sets the absolute parent directory path.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the file name without extension.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the file extension including the leading dot.
        /// </summary>
        public string Extension { get; set; }
    }
}
