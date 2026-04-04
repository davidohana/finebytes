namespace Mfr.Models
{
    /// <summary>
    /// Represents one rename candidate with original and preview metadata.
    /// </summary>
    /// <param name="original">Original immutable file snapshot.</param>
    public sealed class RenameItem(FileEntryLite original)
    {
        /// <summary>
        /// Gets the original immutable file snapshot.
        /// </summary>
        public FileEntryLite Original { get; } = original;

        /// <summary>
        /// Gets the current preview snapshot after filter application.
        /// </summary>
        public FileEntryLite Preview { get; private set; } = original;

        /// <summary>
        /// Resets preview metadata back to the original snapshot.
        /// </summary>
        public void ResetPreview()
        {
            Preview = Original;
        }

        /// <summary>
        /// Updates preview metadata using the provided file-name parts.
        /// </summary>
        /// <param name="prefix">Preview file name without extension.</param>
        /// <param name="extension">Preview extension including leading dot.</param>
        public void SetPreviewName(string prefix, string extension)
        {
            var fullName = prefix + extension;
            var fullPath = Path.Combine(Original.DirectoryPath, fullName);
            Preview = Preview with
            {
                FullPath = fullPath,
                Prefix = prefix,
                Extension = extension,
            };
        }

        /// <summary>
        /// Applies the preview rename on disk for this item.
        /// </summary>
        public void Apply()
        {
            if (string.Equals(Original.FullPath, Preview.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            File.Move(Original.FullPath, Preview.FullPath, overwrite: false);
        }
    }
}
