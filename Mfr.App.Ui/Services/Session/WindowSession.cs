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
        /// <summary>Matches <see cref="Views.MainWindow"/> XAML <c>MinWidth</c>.</summary>
        public const double MinWidth = 800;

        /// <summary>Matches <see cref="Views.MainWindow"/> XAML <c>MinHeight</c>.</summary>
        public const double MinHeight = 500;

        /// <summary>
        /// Restores window size, position, and state from <paramref name="saved"/> when valid.
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="saved">Persisted geometry, or null to skip.</param>
        public static void TryRestore(Window window, SessionWindowState? saved)
        {
            if (saved is null)
                return;

            if (!WindowBounds.TryNormalize(
                    saved.X,
                    saved.Y,
                    saved.Width,
                    saved.Height,
                    _WorkAreas(window),
                    MinWidth,
                    MinHeight,
                    out var x,
                    out var y,
                    out var width,
                    out var height))
                return;

            window.Width = width;
            window.Height = height;
            window.Position = new PixelPoint(x, y);

            window.WindowState = string.Equals(saved.State, "Maximized", StringComparison.OrdinalIgnoreCase) ? WindowState.Maximized : WindowState.Normal;
        }

        /// <summary>
        /// Builds a <see cref="SessionWindowState"/> from tracked normal bounds and current <see cref="Window.WindowState"/>.
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <param name="hasNormalBounds">True when <paramref name="normalWidth"/> / height were observed while Normal.</param>
        /// <param name="normalX">Last known normal-state left edge.</param>
        /// <param name="normalY">Last known normal-state top edge.</param>
        /// <param name="normalWidth">Last known normal-state width.</param>
        /// <param name="normalHeight">Last known normal-state height.</param>
        /// <returns>Session payload ready to persist.</returns>
        public static SessionWindowState Capture(
            Window window,
            bool hasNormalBounds,
            int normalX,
            int normalY,
            double normalWidth,
            double normalHeight)
        {
            var state = window.WindowState == WindowState.Maximized ? "Maximized" : "Normal";

            var width = hasNormalBounds ? normalWidth : window.Width;
            var height = hasNormalBounds ? normalHeight : window.Height;
            var x = hasNormalBounds ? normalX : window.Position.X;
            var y = hasNormalBounds ? normalY : window.Position.Y;

            if (!WindowBounds.TryNormalize(
                    x,
                    y,
                    width,
                    height,
                    _WorkAreas(window),
                    MinWidth,
                    MinHeight,
                    out var nx,
                    out var ny,
                    out var nw,
                    out var nh))
            {
                return new SessionWindowState
                {
                    X = x,
                    Y = y,
                    Width = Math.Max(width, MinWidth),
                    Height = Math.Max(height, MinHeight),
                    State = state,
                };
            }

            return new SessionWindowState
            {
                X = nx,
                Y = ny,
                Width = nw,
                Height = nh,
                State = state,
            };
        }

        private static List<WindowBounds.ScreenRect> _WorkAreas(Window window)
        {
            var screens = window.Screens;
            if (screens is null)
                return [];

            var list = new List<WindowBounds.ScreenRect>();
            foreach (var screen in screens.All)
            {
                var area = screen.WorkingArea;
                list.Add(new WindowBounds.ScreenRect(area.X, area.Y, area.Width, area.Height));
            }

            return list;
        }
    }

    /// <summary>
    /// Pure window-bounds validation (screen intersection + minimum size).
    /// </summary>
    public static class WindowBounds
    {
        /// <summary>
        /// Axis-aligned screen rectangle in pixels.
        /// </summary>
        /// <param name="X">Left edge.</param>
        /// <param name="Y">Top edge.</param>
        /// <param name="Width">Width in pixels.</param>
        /// <param name="Height">Height in pixels.</param>
        public readonly record struct ScreenRect(int X, int Y, int Width, int Height);

        /// <summary>
        /// Clamps size to mins and accepts the rect when it intersects any work area.
        /// </summary>
        /// <param name="x">Proposed left edge in screen pixels.</param>
        /// <param name="y">Proposed top edge in screen pixels.</param>
        /// <param name="width">Proposed width.</param>
        /// <param name="height">Proposed height.</param>
        /// <param name="workAreas">Screen working areas in screen pixels.</param>
        /// <param name="minWidth">Minimum allowed width.</param>
        /// <param name="minHeight">Minimum allowed height.</param>
        /// <param name="normalizedX">Accepted left edge.</param>
        /// <param name="normalizedY">Accepted top edge.</param>
        /// <param name="normalizedWidth">Accepted width.</param>
        /// <param name="normalizedHeight">Accepted height.</param>
        /// <returns>
        /// True when dimensions are finite/positive and the rect intersects a work area (or no work areas were supplied).
        /// </returns>
        public static bool TryNormalize(
            int x,
            int y,
            double width,
            double height,
            IReadOnlyList<ScreenRect> workAreas,
            double minWidth,
            double minHeight,
            out int normalizedX,
            out int normalizedY,
            out double normalizedWidth,
            out double normalizedHeight)
        {
            normalizedX = x;
            normalizedY = y;
            normalizedWidth = 0;
            normalizedHeight = 0;

            if (double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height))
                return false;

            if (width <= 0 || height <= 0)
                return false;

            var w = Math.Max(width, minWidth);
            var h = Math.Max(height, minHeight);

            if (workAreas.Count > 0)
            {
                var windowRight = x + (int)Math.Ceiling(w);
                var windowBottom = y + (int)Math.Ceiling(h);
                var intersects = false;
                foreach (var area in workAreas)
                {
                    var areaRight = area.X + area.Width;
                    var areaBottom = area.Y + area.Height;
                    var overlap = x < areaRight && windowRight > area.X && y < areaBottom && windowBottom > area.Y;
                    if (!overlap)
                        continue;

                    intersects = true;
                    break;
                }

                if (!intersects)
                    return false;
            }

            normalizedX = x;
            normalizedY = y;
            normalizedWidth = w;
            normalizedHeight = h;
            return true;
        }
    }
}
