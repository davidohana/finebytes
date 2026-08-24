using System.Diagnostics;
using System.Runtime.Versioning;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Opens paths via <c>Process.Start</c> and Windows Explorer.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsFileShellOpener : IFileShellOpener
    {
        /// <inheritdoc />
        public void OpenWithDefaultApp(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception)
            {
                // Best-effort shell open.
            }
        }

        /// <inheritdoc />
        public void RevealInFileManager(string path)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true,
                    }
                );
            }
            catch (Exception)
            {
                // Best-effort Explorer reveal.
            }
        }

        /// <inheritdoc />
        public void OpenFolderInFileManager(string folderPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
            }
            catch (Exception)
            {
                // Best-effort folder open.
            }
        }
    }
}
