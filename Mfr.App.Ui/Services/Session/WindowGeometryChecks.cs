using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Shared size and on-screen checks for <see cref="WindowSession"/> and <see cref="DialogSession"/>.
    /// </summary>
    internal static class WindowGeometryChecks
    {
        /// <summary>
        /// Returns true when <paramref name="value"/> is a finite number greater than zero.
        /// </summary>
        /// <param name="value">Width or height in device-independent pixels.</param>
        /// <returns>True when the value is usable as a window dimension.</returns>
        public static bool IsPositiveFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        }

        /// <summary>
        /// Returns true when both dimensions are positive and finite.
        /// </summary>
        /// <param name="width">Width in device-independent pixels.</param>
        /// <param name="height">Height in device-independent pixels.</param>
        /// <returns>True when both dimensions are usable.</returns>
        public static bool IsValidSize(double width, double height)
        {
            return IsPositiveFinite(width) && IsPositiveFinite(height);
        }

        /// <summary>
        /// Returns true when the rectangle intersects a known screen, or when screen info is unavailable.
        /// </summary>
        /// <param name="window">Window whose screen list is used for the hit test.</param>
        /// <param name="x">Left edge in screen pixels.</param>
        /// <param name="y">Top edge in screen pixels.</param>
        /// <param name="width">Width in device-independent pixels (ceiled to pixels for the hit test).</param>
        /// <param name="height">Height in device-independent pixels (ceiled to pixels for the hit test).</param>
        /// <returns>True when the bounds are considered on-screen.</returns>
        public static bool IsOnScreen(Window window, int x, int y, double width, double height)
        {
            var screens = window.Screens;
            if (screens is null || screens.ScreenCount == 0)
            {
                return true;
            }

            var bounds = new PixelRect(x, y, (int)Math.Ceiling(width), (int)Math.Ceiling(height));
            return screens.ScreenFromBounds(bounds) is not null;
        }

        /// <summary>
        /// Returns true when the top-left lies in a screen working area.
        /// <para>
        /// Rejects Windows maximized-frame coords (often slightly negative) that still intersect the
        /// monitor via <see cref="IsOnScreen"/> but place the title bar above the work area.
        /// When screen info is unavailable, still rejects negative <paramref name="y"/> (maximize inset).
        /// </para>
        /// </summary>
        /// <param name="window">Window whose screen list is used for the hit test.</param>
        /// <param name="x">Left edge in screen pixels.</param>
        /// <param name="y">Top edge in screen pixels.</param>
        /// <returns>True when the position is usable as a normal-window origin.</returns>
        public static bool IsPositionInWorkingArea(Window window, int x, int y)
        {
            var screens = window.Screens;
            if (screens is null || screens.ScreenCount == 0)
            {
                return y >= 0;
            }

            var point = new PixelPoint(x, y);
            foreach (var screen in screens.All)
            {
                if (screen.WorkingArea.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
