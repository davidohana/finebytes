namespace Mfr.Models
{
    /// <summary>
    /// Represents the outcome state of a rename item.
    /// </summary>
    public enum RenameStatus
    {
        /// <summary>
        /// Rename operation completed successfully.
        /// </summary>
        Ok,

        /// <summary>
        /// Rename was skipped.
        /// </summary>
        Skipped,

        /// <summary>
        /// Rename was skipped due to destination conflict.
        /// </summary>
        ConflictSkipped,

        /// <summary>
        /// Rename failed due to an error.
        /// </summary>
        Error
    }

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
        /// Gets the latest error captured while generating preview for this item.
        /// </summary>
        public string? PreviewError { get; set; }

        /// <summary>
        /// Gets the latest error captured while committing this item.
        /// </summary>
        public string? CommitError { get; set; }

        /// <summary>
        /// Gets the latest status for this item during preview/commit processing.
        /// </summary>
        public RenameStatus Status { get; set; } = RenameStatus.Skipped;

        /// <summary>
        /// Clears preview metadata so the next preview starts from original data.
        /// </summary>
        public void ResetPreview()
        {
            Preview = null;
        }

        /// <summary>
        /// Clears preview error state before a new preview run.
        /// </summary>
        public void ResetPreviewError()
        {
            PreviewError = null;
        }

        /// <summary>
        /// Clears commit error state before a new commit run.
        /// </summary>
        public void ResetCommitError()
        {
            CommitError = null;
        }

        internal void SetPreviewValue(FileNamePart part, string partValue)
        {
            var sourceFileEntry = _EnsurePreview();
            switch (part)
            {
                case FileNamePart.Prefix:
                    sourceFileEntry.Prefix = partValue;
                    break;
                case FileNamePart.Extension:
                    sourceFileEntry.Extension = partValue;
                    break;
                case FileNamePart.Full:
                    var fullName = Path.GetFileName(partValue);
                    sourceFileEntry.Extension = Path.GetExtension(fullName);
                    sourceFileEntry.Prefix = Path.GetFileNameWithoutExtension(fullName);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown fileNamePart '{part}'.");
            }
        }

        internal void CopyPreviewFromOriginal()
        {
            Preview = _CloneFileEntry(Original);
        }

        /// <summary>
        /// Applies the preview rename on disk for this item.
        /// </summary>
        public void CommitPreview()
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

        private FileEntryLite _EnsurePreview()
        {
            if (Preview is not null)
            {
                return Preview;
            }

            Preview = _CloneFileEntry(Original);
            return Preview;
        }

        private static FileEntryLite _CloneFileEntry(FileEntryLite source)
        {
            return new FileEntryLite(
                source.GlobalIndex,
                source.InFolderIndex,
                source.FullPath,
                source.DirectoryPath,
                source.Prefix,
                source.Extension);
        }
    }
}
