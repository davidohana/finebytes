namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Explorer-compatible file clipboard (CF_HDROP + Preferred DropEffect) for File List Cut/Copy/Paste.
    /// </summary>
    public interface IFileClipboard
    {
        /// <summary>
        /// Gets paths currently marked as cut for File List ghosting.
        /// <para>
        /// Empty after <see cref="SetCopy"/> or when no cut payload is active.
        /// </para>
        /// </summary>
        IReadOnlySet<string> CutPaths { get; }

        /// <summary>
        /// Raised when <see cref="CutPaths"/> or the pasteable payload may have changed.
        /// </summary>
        event EventHandler? Changed;

        /// <summary>
        /// Gets whether the clipboard currently holds pasteable filesystem paths.
        /// </summary>
        bool HasPasteableFiles { get; }

        /// <summary>
        /// Places <paramref name="paths"/> on the clipboard as a Copy (DropEffect Copy) and clears cut marks.
        /// </summary>
        /// <param name="paths">Absolute file or folder paths.</param>
        void SetCopy(IReadOnlyList<string> paths);

        /// <summary>
        /// Places <paramref name="paths"/> on the clipboard as a Cut (DropEffect Move) and updates cut marks.
        /// </summary>
        /// <param name="paths">Absolute file or folder paths.</param>
        void SetCut(IReadOnlyList<string> paths);

        /// <summary>
        /// Tries to read a file paste payload from the clipboard.
        /// </summary>
        /// <param name="paste">Paste paths and whether Move is preferred when present.</param>
        /// <returns><see langword="true"/> when at least one filesystem path is available.</returns>
        bool TryGetPaste(out FileClipboardPaste paste);
    }
}
