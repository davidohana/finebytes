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

        internal void SetPreviewValue(FileNamePart part, string partValue)
        {
            var sourceFileEntry = Preview ?? Original;
            switch (part)
            {
                case FileNamePart.Prefix:
                    _SetPreviewFileEntry(sourceFileEntry, partValue, sourceFileEntry.Extension);
                    return;
                case FileNamePart.Extension:
                    _SetPreviewFileEntry(sourceFileEntry, sourceFileEntry.Prefix, partValue);
                    return;
                case FileNamePart.Full:
                    var fullName = Path.GetFileName(partValue);
                    var extension = Path.GetExtension(fullName);
                    var prefix = Path.GetFileNameWithoutExtension(fullName);
                    _SetPreviewFileEntry(sourceFileEntry, prefix, extension);
                    return;
                default:
                    throw new InvalidOperationException($"Unknown fileNamePart '{part}'.");
            }
        }

        internal void CopyPreviewFromOriginal()
        {
            Preview = Original;
        }

        private void _SetPreviewFileEntry(FileEntryLite source, string prefix, string extension)
        {
            var fullName = prefix + extension;
            var fullPath = Path.Combine(source.DirectoryPath, fullName);
            Preview = source with
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
