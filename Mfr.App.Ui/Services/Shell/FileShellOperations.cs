namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Creates the default <see cref="IFileShellOperations"/> for the current OS.
    /// </summary>
    public static class FileShellOperations
    {
        /// <summary>
        /// Returns a Windows <c>IFileOperation</c>-backed implementation on Windows; otherwise a no-op.
        /// </summary>
        /// <returns>Shell file operations suitable for the host OS.</returns>
        public static IFileShellOperations CreateDefault()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsFileShellOperations();
            }

            return NullFileShellOperations.Instance;
        }
    }
}
