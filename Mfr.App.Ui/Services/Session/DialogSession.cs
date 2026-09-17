using System.Runtime.CompilerServices;
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

            // Maximize only after restore bounds apply — maximize-only entries (zero size) leave XAML defaults.
            if (
                !WindowGeometryChecks.TryApplyNormalBounds(
                    window,
                    saved.X,
                    saved.Y,
                    saved.Width,
                    saved.Height,
                    applyHeight: mode == DialogGeometryMode.SizeAndPosition,
                    setManualStartupLocation: true
                )
            )
            {
                return;
            }

            if (saved.Maximized)
            {
                window.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// Writes current size, position, and maximized flag into root <see cref="ConfigStore.Dialogs"/>
        /// when remember is on.
        /// </summary>
        /// <remarks>
        /// <para>Always stores height; <see cref="DialogGeometryMode.WidthAndPosition"/> restore ignores it.</para>
        /// <para>
        /// When maximized, keeps prior normal restore bounds and only sets <see cref="WindowGeometryPrefs.Maximized"/>;
        /// Windows maximized-frame coords are not written. Maximize without prior usable restore bounds is not persisted.
        /// </para>
        /// </remarks>
        private static void _Capture(Window window, string id)
        {
            if (!_RememberWindowState())
            {
                return;
            }

            var isMaximized = window.WindowState == WindowState.Maximized;
            if (window.WindowState is not (WindowState.Normal or WindowState.Maximized))
            {
                return;
            }

            var dialogs = ConfigStore.EnsureDialogs();
            dialogs.TryGetValue(id, out var entry);

            if (isMaximized)
            {
                if (
                    entry is null
                    || !WindowGeometryChecks.IsUsableRestoreBounds(window, entry.X, entry.Y, entry.Width, entry.Height)
                )
                {
                    return;
                }

                entry.Maximized = true;
                return;
            }

            var width = window.Width;
            var height = window.Height;
            var x = window.Position.X;
            var y = window.Position.Y;
            if (
                !WindowGeometryChecks.IsValidSize(width, height)
                || !WindowGeometryChecks.IsOnScreen(window, x, y, width, height)
                || !WindowGeometryChecks.IsPositionInWorkingArea(window, x, y)
            )
            {
                return;
            }

            entry ??= new WindowGeometryPrefs();
            dialogs[id] = entry;
            entry.X = x;
            entry.Y = y;
            entry.Width = width;
            entry.Height = height;
            entry.Maximized = false;
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
