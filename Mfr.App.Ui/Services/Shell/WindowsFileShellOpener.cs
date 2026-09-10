using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Opens paths via <c>Process.Start</c>, Windows Explorer, and shell verbs.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsFileShellOpener : IFileShellOpener
    {
        private const int SwShow = 5;
        private const uint SeeMaskInvokeIdList = 12;

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

        /// <inheritdoc />
        public void ShowProperties(string path)
        {
            try
            {
                var info = new ShellExecuteInfo
                {
                    CbSize = Marshal.SizeOf<ShellExecuteInfo>(),
                    FMask = SeeMaskInvokeIdList,
                    LpVerb = "properties",
                    LpFile = path,
                    NShow = SwShow,
                };
                _ = NativeMethods.ShellExecuteEx(ref info);
            }
            catch (Exception)
            {
                // Best-effort property sheet.
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShellExecuteInfo
        {
            public int CbSize;
            public uint FMask;
            public IntPtr Hwnd;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? LpVerb;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? LpFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? LpParameters;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? LpDirectory;

            public int NShow;
            public IntPtr HInstApp;
            public IntPtr LpIDList;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? LpClass;

            public IntPtr HkeyClass;
            public uint DwHotKey;
            public IntPtr HIcon;
            public IntPtr HProcess;
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        }
    }
}
