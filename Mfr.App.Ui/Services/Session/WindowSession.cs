using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Applies and captures main-window geometry for <see cref="ConfigStore.MainWindow"/>.
    /// </summary>
    internal static class WindowSession
    {
        /// <summary>Matches <see cref="Views.MainWindow.MainWindow"/> XAML <c>MinWidth</c>.</summary>
        private const double MinWidth = 800;

        /// <summary>Matches <see cref="Views.MainWindow.MainWindow"/> XAML <c>MinHeight</c>.</summary>
        private const double MinHeight = 500;

        /// <summary>Default size as a fraction of the primary screen working area.</summary>
        private const double DefaultScreenFraction = 2.0 / 3.0;

        /// <summary>
        /// Restores window size, position, and state from <paramref name="saved"/> when valid.
        /// <para>
        /// When the saved state is maximized, valid normal size/position are applied first as restore
        /// bounds, then maximization is set. Otherwise size and position are applied when valid and
        /// on-screen.
        /// </para>
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="saved">Persisted geometry, or null to skip.</param>
        /// <returns>True when a saved layout was applied; false when the caller should use defaults.</returns>
        public static bool TryRestore(Window window, MainWindowPrefs? saved)
        {
            if (saved is null)
            {
                return false;
            }

            var isMaximized = string.Equals(saved.State, "Maximized", StringComparison.OrdinalIgnoreCase);
            if (isMaximized)
            {
                _TryApplyNormalGeometry(window, saved);
                window.WindowState = WindowState.Maximized;
                return true;
            }

            if (!_TryApplyNormalGeometry(window, saved))
            {
                return false;
            }

            window.WindowState = WindowState.Normal;
            return true;
        }

        /// <summary>
        /// Applies first-run geometry: about two-thirds of the primary working area, centered on screen.
        /// <para>
        /// When screen info is not ready yet, centers via <see cref="WindowStartupLocation.CenterScreen"/>
        /// and finishes sizing once the window has opened.
        /// </para>
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        public static void ApplyDefault(Window window)
        {
            window.WindowState = WindowState.Normal;

            if (_TryApplyDefaultSizeAndCenter(window))
            {
                return;
            }

            // Screens is often unavailable until the window is shown (ApplyDefault runs at startup).
            // Center with the XAML size for first paint, then size to 2/3 and re-center on Opened.
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            void OnOpened(object? sender, EventArgs e)
            {
                window.Opened -= OnOpened;
                _TryApplyDefaultSizeAndCenter(window);
            }

            window.Opened += OnOpened;
        }

        /// <summary>
        /// Builds a <see cref="MainWindowPrefs"/> from the window's current geometry and state.
        /// <para>
        /// When maximized, keeps the previous normal size/position from <see cref="ConfigStore.MainWindow"/>
        /// as restore bounds instead of Windows maximized-frame coords (often slightly negative).
        /// </para>
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <returns>Session payload ready to persist (splitters are captured separately).</returns>
        public static MainWindowPrefs Capture(Window window)
        {
            var isMaximized = window.WindowState == WindowState.Maximized;
            if (isMaximized)
            {
                var prior = ConfigStore.MainWindow;
                if (
                    prior is not null
                    && WindowGeometryChecks.IsValidSize(prior.Width, prior.Height)
                    && WindowGeometryChecks.IsPositionInWorkingArea(window, prior.X, prior.Y)
                )
                {
                    return new MainWindowPrefs
                    {
                        X = prior.X,
                        Y = prior.Y,
                        Width = prior.Width,
                        Height = prior.Height,
                        State = "Maximized",
                    };
                }
            }

            return new MainWindowPrefs
            {
                X = window.Position.X,
                Y = window.Position.Y,
                Width = window.Width,
                Height = window.Height,
                State = isMaximized ? "Maximized" : "Normal",
            };
        }

        private static bool _TryApplyNormalGeometry(Window window, MainWindowPrefs saved)
        {
            if (!WindowGeometryChecks.IsValidSize(saved.Width, saved.Height))
            {
                return false;
            }

            if (!WindowGeometryChecks.IsOnScreen(window, saved.X, saved.Y, saved.Width, saved.Height))
            {
                return false;
            }

            if (!WindowGeometryChecks.IsPositionInWorkingArea(window, saved.X, saved.Y))
            {
                return false;
            }

            window.Width = saved.Width;
            window.Height = saved.Height;
            window.Position = new PixelPoint(saved.X, saved.Y);
            return true;
        }

        private static bool _TryApplyDefaultSizeAndCenter(Window window)
        {
            var screen = _TryGetPrimaryScreen(window);
            if (screen is null)
            {
                return false;
            }

            var area = screen.WorkingArea;
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1.0;
            var widthDip = area.Width / scaling * DefaultScreenFraction;
            var heightDip = area.Height / scaling * DefaultScreenFraction;
            var width = Math.Max(MinWidth, widthDip);
            var height = Math.Max(MinHeight, heightDip);
            var pixelWidth = (int)Math.Round(width * scaling);
            var pixelHeight = (int)Math.Round(height * scaling);

            window.Width = width;
            window.Height = height;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            var centeredX = area.X + ((area.Width - pixelWidth) / 2);
            var centeredY = area.Y + ((area.Height - pixelHeight) / 2);
            window.Position = new PixelPoint(centeredX, centeredY);
            return true;
        }

        private static Screen? _TryGetPrimaryScreen(Window window)
        {
            var screens = window.Screens;
            if (screens is null || screens.ScreenCount == 0)
            {
                return null;
            }

            return screens.Primary ?? (screens.All.Count > 0 ? screens.All[0] : null);
        }
    }
}
