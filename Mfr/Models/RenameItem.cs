namespace Mfr.Models
{
    /// <summary>
    /// Represents error details for preview/commit processing.
    /// </summary>
    /// <param name="Message">Human-readable error message.</param>
    /// <param name="Cause">Optional underlying exception instance.</param>
    public sealed record RenameItemError(string Message, Exception? Cause = null);

    /// <summary>
    /// Represents the outcome state of a rename item.
    /// </summary>
    public enum RenameStatus
    {
        /// <summary>
        /// Item has not been previewed/committed yet.
        /// </summary>
        Init,

        /// <summary>
        /// Preview generated a destination different from source.
        /// </summary>
        PreviewOk,

        /// <summary>
        /// Preview failed with an error.
        /// </summary>
        PreviewError,

        /// <summary>
        /// Commit operation completed successfully.
        /// </summary>
        CommitOk,

        /// <summary>
        /// Commit was skipped.
        /// </summary>
        CommitSkipped,

        /// <summary>
        /// Commit failed due to an error.
        /// </summary>
        CommitError
    }

    /// <summary>
    /// Represents one rename candidate with original and preview metadata.
    /// </summary>
    /// <param name="original">Original immutable file snapshot.</param>
    public sealed class RenameItem(FileMeta original)
    {
        /// <summary>
        /// Gets the original immutable file snapshot.
        /// </summary>
        public FileMeta Original { get; private set; } = original;

        /// <summary>
        /// Gets the current preview snapshot after filter application.
        /// A <c>null</c> value means no preview has been generated yet.
        /// </summary>
        public FileMeta? Preview { get; private set; }

        /// <summary>
        /// Gets the latest error captured while generating preview for this item.
        /// </summary>
        public RenameItemError? PreviewError { get; set; }

        /// <summary>
        /// Gets the latest error captured while committing this item.
        /// </summary>
        public RenameItemError? CommitError { get; set; }

        /// <summary>
        /// Gets the latest status for this item during preview/commit processing.
        /// </summary>
        public RenameStatus Status { get; set; } = RenameStatus.Init;

        /// <summary>
        /// Resets transient preview/commit state for a fresh processing cycle.
        /// </summary>
        public void ResetState()
        {
            Preview = null;
            PreviewError = null;
            CommitError = null;
            Status = RenameStatus.Init;
        }

        internal void ClearPreview()
        {
            Preview = null;
        }

        internal void SetPreviewValue(FileNamePart part, string partValue)
        {
            if (!HasPreview())
            {
                Preview = Original.Clone();
            }

            var previewFileMeta = Preview!;
            switch (part)
            {
                case FileNamePart.Prefix:
                    previewFileMeta.Prefix = partValue;
                    break;
                case FileNamePart.Extension:
                    previewFileMeta.Extension = partValue;
                    break;
                case FileNamePart.Full:
                    var fullName = Path.GetFileName(partValue);
                    previewFileMeta.Extension = Path.GetExtension(fullName);
                    previewFileMeta.Prefix = Path.GetFileNameWithoutExtension(fullName);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown fileNamePart '{part}'.");
            }
        }

        internal void CopyPreviewFromOriginal()
        {
            Preview = Original.Clone();
        }

        internal void SetPreviewError(string message, Exception? cause)
        {
            PreviewError = new RenameItemError(Message: message, Cause: cause);
            Status = RenameStatus.PreviewError;
        }

        internal bool HasPreview()
        {
            return Preview is not null;
        }

        internal bool IsPreviewPathSameAsOriginal()
        {
            var preview = Preview;
            return HasPreview()
                && string.Equals(Original.FullPath, preview!.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies the preview rename on disk for this item.
        /// </summary>
        public void Commit()
        {
            if (!HasPreview())
            {
                throw new InvalidOperationException("Preview is not set. Run preview before apply.");
            }

            if (IsPreviewPathSameAsOriginal())
            {
                Preview = null;
                return;
            }

            var preview = Preview!;
            File.Move(Original.FullPath, preview.FullPath, overwrite: false);
            Original = preview;
            Preview = null;
        }

    }
}
