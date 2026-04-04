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
        public FileEntryLite Original { get; private set; } = original;

        /// <summary>
        /// Gets the current preview snapshot after filter application.
        /// A <c>null</c> value means no preview has been generated yet.
        /// </summary>
        public FileEntryLite? Preview { get; private set; }

        /// <summary>
        /// Clears preview metadata so the next preview starts from original data.
        /// </summary>
        public void ResetPreview()
        {
            Preview = null;
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
            Preview = Original with
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
            if (Preview is null)
            {
                throw new InvalidOperationException("Preview is not set. Run preview before apply.");
            }

            if (string.Equals(Original.FullPath, Preview.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                Preview = null;
                return;
            }

            File.Move(Original.FullPath, Preview.FullPath, overwrite: false);
            Original = Preview;
            Preview = null;
        }
    }
}
