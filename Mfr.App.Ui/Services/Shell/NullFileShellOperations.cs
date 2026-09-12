namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// No-op shell file operations for tests and platforms without <c>IFileOperation</c>.
    /// </summary>
    public sealed class NullFileShellOperations : IFileShellOperations
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullFileShellOperations Instance { get; } = new();

        /// <inheritdoc />
        public FileShellOperationResult Delete(IReadOnlyList<string> paths, bool recycle, IntPtr ownerHwnd = default)
        {
            return FileShellOperationResult.Succeeded;
        }

        /// <inheritdoc />
        public FileShellOperationResult Copy(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            return FileShellOperationResult.Succeeded;
        }

        /// <inheritdoc />
        public FileShellOperationResult Move(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            return FileShellOperationResult.Succeeded;
        }
    }
}
