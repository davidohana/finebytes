using Mfr.Utils;

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
        public FileMeta Original { get; internal set; } = original;

        /// <summary>
        /// Gets the current preview snapshot after filter application.
        /// </summary>
        public FileMeta Preview { get; private set; } = original.Clone();

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
        /// Gets or sets the word-separator character for the current filter chain pass.
        /// Reset to U+0020 SPACE at the start of each preview cycle; updated when a
        /// <c>SpaceCharacter</c> filter runs.
        /// </summary>
        internal char WordSeparator { get; set; } = ' ';

        /// <summary>
        /// Gets or sets characters that mark sentence endings for the current filter chain pass.
        /// Reset to <c>".!?"</c> at the start of each preview cycle; updated when a
        /// <c>SentenceEndCharacters</c> filter runs.
        /// </summary>
        internal string SentenceEndChars { get; set; } = ".!?";

        /// <summary>
        /// Resets transient preview/commit state for a fresh processing cycle.
        /// </summary>
        public void ResetState()
        {
            Preview = Original.Clone();
            PreviewError = null;
            CommitError = null;
            Status = RenameStatus.Init;
            WordSeparator = ' ';
            SentenceEndChars = ".!?";
        }

        internal void ClearPreview()
        {
            Preview = Original.Clone();
            WordSeparator = ' ';
            SentenceEndChars = ".!?";
        }

        internal void SetPreviewError(string message, Exception? cause)
        {
            PreviewError = new RenameItemError(Message: message, Cause: cause);
            Status = RenameStatus.PreviewError;
        }

        /// <summary>
        /// Whether the preview path equals the original path byte-for-byte (including case).
        /// </summary>
        /// <returns><c>true</c> when the strings are identical.</returns>
        internal bool IsPreviewPathUnchanged()
        {
            return string.Equals(Original.FullPath, Preview.FullPath, StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether the preview path resolves to the same on-disk entry as the original under the host filesystem.
        /// </summary>
        /// <returns><c>true</c> when both paths refer to the same on-disk entry, even when textual casing differs.</returns>
        internal bool IsPreviewPathSameOnDisk()
        {
            return PathRelations.SameOnDisk(Original.FullPath, Preview.FullPath);
        }

        /// <summary>
        /// Whether preview differs from the original snapshot (path, filesystem attributes, or timestamps).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Path differences are evaluated via ordinal comparison so case-only renames count as a change
        /// even on case-insensitive filesystems.
        /// </para>
        /// </remarks>
        public bool HasPreviewChanges()
        {
            return !IsPreviewPathUnchanged()
                || Original.Attributes != Preview.Attributes
                || Original.CreationTime != Preview.CreationTime
                || Original.LastWriteTime != Preview.LastWriteTime
                || Original.LastAccessTime != Preview.LastAccessTime;
        }
    }
}
