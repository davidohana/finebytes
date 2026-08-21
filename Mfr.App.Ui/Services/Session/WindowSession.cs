using Avalonia;
using Avalonia.Controls;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Applies and captures main-window geometry for <see cref="SessionStore"/>.
    /// </summary>
    internal static class WindowSession
    {
        /// <summary>
        /// Restores window size, position, and state from <paramref name="saved"/> when valid.
        /// <para>
        /// When the saved state is maximized, only maximization is restored (XAML default size remains).
        /// Otherwise size and position are applied when they are valid and on-screen.
        /// </para>
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="saved">Persisted geometry, or null to skip.</param>
        public static void TryRestore(Window window, SessionWindowState? saved)
        {
            if (saved is null)
                return;

            var isMaximized = string.Equals(saved.State, "Maximized", StringComparison.OrdinalIgnoreCase);
            if (isMaximized)
            {
                window.WindowState = WindowState.Maximized;
                return;
            }

            if (!_IsValidSize(saved.Width, saved.Height))
                return;

            if (!_IsOnScreen(window, saved.X, saved.Y, saved.Width, saved.Height))
                return;

            window.Width = saved.Width;
            window.Height = saved.Height;
            window.Position = new PixelPoint(saved.X, saved.Y);
            window.WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Builds a <see cref="SessionWindowState"/> from the window's current geometry and state.
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <returns>Session payload ready to persist.</returns>
        public static SessionWindowState Capture(Window window)
        {
            return new SessionWindowState
            {
                X = window.Position.X,
                Y = window.Position.Y,
                Width = window.Width,
                Height = window.Height,
                State = window.WindowState == WindowState.Maximized ? "Maximized" : "Normal",
            };
        }

        private static bool _IsValidSize(double width, double height)
        {
            if (double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height))
                return false;

            return width > 0 && height > 0;
        }

        private static bool _IsOnScreen(Window window, int x, int y, double width, double height)
        {
            var screens = window.Screens;
            if (screens is null || screens.ScreenCount == 0)
                return true;

            var bounds = new PixelRect(x, y, (int)Math.Ceiling(width), (int)Math.Ceiling(height));
            return screens.ScreenFromBounds(bounds) is not null;
        }
    }
}
