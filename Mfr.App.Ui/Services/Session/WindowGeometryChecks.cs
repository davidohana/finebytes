using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Shared size, on-screen, apply, and maximize-restore checks for <see cref="WindowSession"/> and
    /// <see cref="DialogSession"/>.
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

        /// <summary>
        /// Returns true when prior normal bounds can be kept as maximized restore geometry.
        /// </summary>
        /// <param name="window">Window used for working-area checks.</param>
        /// <param name="x">Prior left edge in screen pixels.</param>
        /// <param name="y">Prior top edge in screen pixels.</param>
        /// <param name="width">Prior width in device-independent pixels.</param>
        /// <param name="height">Prior height in device-independent pixels.</param>
        /// <returns>True when size is valid and the origin is in a working area.</returns>
        public static bool IsUsableRestoreBounds(Window window, int x, int y, double width, double height)
        {
            return IsValidSize(width, height) && IsPositionInWorkingArea(window, x, y);
        }

        /// <summary>
        /// Applies normal size and position when the saved bounds are usable.
        /// </summary>
        /// <param name="window">Window to configure.</param>
        /// <param name="x">Left edge in screen pixels.</param>
        /// <param name="y">Top edge in screen pixels.</param>
        /// <param name="width">Width in device-independent pixels.</param>
        /// <param name="height">Height in device-independent pixels (ignored when <paramref name="applyHeight"/> is false).</param>
        /// <param name="applyHeight">When false, only width and position are set (width-only dialogs).</param>
        /// <param name="setManualStartupLocation">When true, sets <see cref="WindowStartupLocation.Manual"/>.</param>
        /// <returns>True when geometry was applied.</returns>
        public static bool TryApplyNormalBounds(
            Window window,
            int x,
            int y,
            double width,
            double height,
            bool applyHeight = true,
            bool setManualStartupLocation = false
        )
        {
            if (!IsPositiveFinite(width))
            {
                return false;
            }

            if (applyHeight && !IsPositiveFinite(height))
            {
                return false;
            }

            // Width-only restore ignores saved height; use the window's current height for the on-screen check.
            var heightForBounds = applyHeight ? height : (IsPositiveFinite(window.Height) ? window.Height : 1);
            if (!IsOnScreen(window, x, y, width, heightForBounds))
            {
                return false;
            }

            if (!IsPositionInWorkingArea(window, x, y))
            {
                return false;
            }

            if (setManualStartupLocation)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
            }

            window.Width = width;
            if (applyHeight)
            {
                window.Height = height;
            }

            window.Position = new PixelPoint(x, y);
            return true;
        }
    }
}
