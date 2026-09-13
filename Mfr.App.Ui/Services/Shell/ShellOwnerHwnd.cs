using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Resolves an owner HWND for shell UI (progress / confirms) from the desktop main window.
    /// </summary>
    public static class ShellOwnerHwnd
    {
        /// <summary>
        /// Returns the desktop main window platform handle when available; otherwise <see cref="IntPtr.Zero"/>.
        /// </summary>
        /// <returns>Win32 HWND on Windows with a realized main window; zero when unknown.</returns>
        public static IntPtr TryGetMainWindowHandle()
        {
            if (
                Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow is null
            )
            {
                return IntPtr.Zero;
            }

            return desktop.MainWindow.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        }
    }
}
