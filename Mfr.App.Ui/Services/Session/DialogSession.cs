using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Applies and captures resizable-modal geometry under root <see cref="ConfigStore.Dialogs"/>.
    /// </summary>
    internal static class DialogSession
    {
        private static readonly ConditionalWeakTable<Window, object> s_attached = [];

        /// <summary>
        /// Restores saved geometry when remember is on, and captures on close into <see cref="ConfigStore"/>.
        /// <para>
        /// Disk flush stays with existing <see cref="ConfigStore.TrySave"/> paths (app close / Options OK).
        /// Invalid or missing entries leave the dialog's XAML <c>CenterOwner</c> defaults.
        /// </para>
        /// </summary>
        /// <param name="window">Modal dialog to configure.</param>
        /// <param name="id">Stable key under root <c>dialogs</c> (see <see cref="DialogIds"/>).</param>
        /// <param name="mode">Which size fields to restore (height is ignored for width-only dialogs).</param>
        public static void Attach(
            Window window,
            string id,
            DialogGeometryMode mode = DialogGeometryMode.SizeAndPosition
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            if (s_attached.TryGetValue(window, out _))
            {
                return;
            }

            s_attached.Add(window, string.Empty);

            if (_RememberWindowState())
            {
                _TryRestore(window, id, mode);
            }

            window.Closing += (_, _) => _Capture(window, id);
        }

        private static void _TryRestore(Window window, string id, DialogGeometryMode mode)
        {
            var saved = _TryGetSaved(id);
            if (saved is null)
            {
                return;
            }

            var restoreHeight = mode == DialogGeometryMode.SizeAndPosition;
            if (!WindowGeometryChecks.IsPositiveFinite(saved.Width))
            {
                return;
            }

            if (restoreHeight && !WindowGeometryChecks.IsPositiveFinite(saved.Height))
            {
                return;
            }

            // Width-only restore ignores saved height; use the dialog's current height for the on-screen check.
            var heightForBounds = restoreHeight
                ? saved.Height
                : (WindowGeometryChecks.IsPositiveFinite(window.Height) ? window.Height : 1);
            if (!WindowGeometryChecks.IsOnScreen(window, saved.X, saved.Y, saved.Width, heightForBounds))
            {
                return;
            }

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Width = saved.Width;
            if (restoreHeight)
            {
                window.Height = saved.Height;
            }

            window.Position = new PixelPoint(saved.X, saved.Y);
        }

        /// <summary>
        /// Writes current size and position into root <see cref="ConfigStore.Dialogs"/> when remember is on.
        /// <para>Always stores height; <see cref="DialogGeometryMode.WidthAndPosition"/> restore ignores it.</para>
        /// </summary>
        private static void _Capture(Window window, string id)
        {
            if (!_RememberWindowState())
            {
                return;
            }

            var width = window.Width;
            var height = window.Height;
            if (!WindowGeometryChecks.IsValidSize(width, height))
            {
                return;
            }

            var x = window.Position.X;
            var y = window.Position.Y;
            if (!WindowGeometryChecks.IsOnScreen(window, x, y, width, height))
            {
                return;
            }

            var dialogs = ConfigStore.EnsureDialogs();
            if (!dialogs.TryGetValue(id, out var entry))
            {
                entry = new WindowGeometryPrefs();
                dialogs[id] = entry;
            }

            entry.X = x;
            entry.Y = y;
            entry.Width = width;
            entry.Height = height;
        }

        private static WindowGeometryPrefs? _TryGetSaved(string id)
        {
            var dialogs = ConfigStore.Dialogs;
            if (dialogs is null)
            {
                return null;
            }

            return dialogs.TryGetValue(id, out var saved) ? saved : null;
        }

        private static bool _RememberWindowState()
        {
            return ConfigStore.Options.RememberWindowState;
        }
    }
}
