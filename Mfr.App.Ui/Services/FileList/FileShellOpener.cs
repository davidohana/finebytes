namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Creates the default <see cref="IFileShellOpener"/> for the current OS.
    /// </summary>
    public static class FileShellOpener
    {
        /// <summary>
        /// Returns a Windows Explorer-backed opener on Windows, otherwise a no-op opener.
        /// </summary>
        /// <returns>A shell opener suitable for the host OS.</returns>
        public static IFileShellOpener CreateDefault()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsFileShellOpener();
            }

            return NullFileShellOpener.Instance;
        }
    }
}
