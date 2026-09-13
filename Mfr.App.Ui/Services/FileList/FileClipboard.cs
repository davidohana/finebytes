namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Creates the default <see cref="IFileClipboard"/> for the current OS.
    /// </summary>
    public static class FileClipboard
    {
        /// <summary>
        /// Returns a Win32 CF_HDROP implementation on Windows; otherwise a no-op that still tracks cut marks.
        /// </summary>
        /// <returns>File clipboard suitable for the host OS.</returns>
        public static IFileClipboard CreateDefault()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsFileClipboard();
            }

            return new NullFileClipboard();
        }
    }
}
