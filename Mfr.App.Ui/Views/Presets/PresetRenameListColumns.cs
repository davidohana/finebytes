using Avalonia.Controls;
using Mfr.App.Ui.ViewModels;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Captures and applies Rename List visible columns via the owning <see cref="MainWindowViewModel"/>.
    /// <para>
    /// Used by preset Save and Load so hosts share one TopLevel reach-in instead of duplicating it.
    /// </para>
    /// </summary>
    internal static class PresetRenameListColumns
    {
        /// <summary>
        /// Captures current Rename List visible columns from the main window that owns <paramref name="host"/>.
        /// </summary>
        /// <param name="host">Any control under the main window (e.g. Applied Filters pane).</param>
        /// <returns>Current columns, or an empty list when the main window is unavailable.</returns>
        public static IReadOnlyList<RenameListVisibleColumnSpec> Capture(Control host)
        {
            ArgumentNullException.ThrowIfNull(host);

            if (TopLevel.GetTopLevel(host) is Window { DataContext: MainWindowViewModel main })
            {
                return main.RenameListViewModel.CaptureVisibleColumnSpecs();
            }

            return [];
        }

        /// <summary>
        /// Applies preset columns to the Rename List when present; no-op when <paramref name="columns"/> is null.
        /// </summary>
        /// <param name="host">Any control under the main window (e.g. Applied Filters pane).</param>
        /// <param name="columns">
        /// Columns from the preset, or <see langword="null"/> to leave the current Rename List unchanged.
        /// </param>
        public static void ApplyIfPresent(Control host, IReadOnlyList<RenameListVisibleColumnSpec>? columns)
        {
            ArgumentNullException.ThrowIfNull(host);

            if (columns is null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(host) is Window { DataContext: MainWindowViewModel main })
            {
                main.RenameListViewModel.ApplyVisibleColumnSpecs(columns);
            }
        }
    }
}
