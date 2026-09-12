namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Deletes, copies, and moves filesystem items via the OS shell (progress and confirms).
    /// </summary>
    public interface IFileShellOperations
    {
        /// <summary>
        /// Deletes <paramref name="paths"/>, optionally sending them to the Recycle Bin.
        /// </summary>
        /// <param name="paths">Absolute file or folder paths.</param>
        /// <param name="recycle">
        /// When <see langword="true"/>, prefer Recycle Bin / undo; when <see langword="false"/>, permanent delete.
        /// </param>
        /// <param name="ownerHwnd">Optional owner window for shell UI modality; use <see cref="IntPtr.Zero"/> when unknown.</param>
        /// <returns>Shell outcome for status handling.</returns>
        FileShellOperationResult Delete(IReadOnlyList<string> paths, bool recycle, IntPtr ownerHwnd = default);

        /// <summary>
        /// Copies <paramref name="paths"/> into <paramref name="destinationDirectory"/>.
        /// </summary>
        /// <param name="paths">Absolute file or folder paths to copy.</param>
        /// <param name="destinationDirectory">Existing destination folder path.</param>
        /// <param name="ownerHwnd">Optional owner window for shell UI modality; use <see cref="IntPtr.Zero"/> when unknown.</param>
        /// <returns>Shell outcome for status handling.</returns>
        FileShellOperationResult Copy(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        );

        /// <summary>
        /// Moves <paramref name="paths"/> into <paramref name="destinationDirectory"/>.
        /// </summary>
        /// <param name="paths">Absolute file or folder paths to move.</param>
        /// <param name="destinationDirectory">Existing destination folder path.</param>
        /// <param name="ownerHwnd">Optional owner window for shell UI modality; use <see cref="IntPtr.Zero"/> when unknown.</param>
        /// <returns>Shell outcome for status handling.</returns>
        FileShellOperationResult Move(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        );
    }
}
