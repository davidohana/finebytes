using Avalonia.Controls;
using Mfr.App.Ui.Services.FileList;
using Mfr.Engine.Config;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Restores and saves UI session sections on <see cref="ConfigStore"/>.
    /// </summary>
    internal static class UiSessionPersistence
    {
        /// <summary>
        /// Restores remembered main-window layout from <see cref="ConfigStore.MainWindow"/>.
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="panes">Pane grids for splitter restore.</param>
        /// <remarks>
        /// File List and Rename List session fields are restored separately via pane apply/capture methods.
        /// </remarks>
        public static void TryRestore(Window window, MainWindowPaneGrids panes)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);

            var windowRestored = false;

            if (ConfigStore.Options.RememberWindowState)
            {
                windowRestored = WindowSession.TryRestore(window, ConfigStore.MainWindow);

                SplitterSession.TryRestore(panes, ConfigStore.MainWindow?.Splitters);
            }

            if (!windowRestored)
            {
                WindowSession.ApplyDefault(window);
            }
        }

        /// <summary>
        /// Writes UI session sections into <see cref="ConfigStore"/> then saves the whole prefs document:
        /// window/folder when their remember flags are on; File List masks/view and Rename List always.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="panes">Pane grids for splitter capture.</param>
        /// <param name="fileList">
        /// File List session fields to persist, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="renameList">
        /// Rename List session fields, or <see langword="null"/> to leave the saved section unchanged.
        /// </param>
        public static void SaveOnClose(
            Window window,
            MainWindowPaneGrids panes,
            FileListPrefs? fileList,
            RenameListPrefs? renameList = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);

            try
            {
                var options = ConfigStore.Options;

                if (options.RememberWindowState)
                {
                    var captured = WindowSession.Capture(window);
                    if (captured is not null)
                    {
                        captured.Splitters = SplitterSession.Capture(panes);
                        ConfigStore.MainWindow = captured;
                    }
                    else if (ConfigStore.MainWindow is { } existing)
                    {
                        // Maximized without usable restore bounds: keep prior geometry, refresh splitters.
                        existing.Splitters = SplitterSession.Capture(panes);
                    }
                }

                if (fileList is not null)
                {
                    var toSave = fileList;
                    if (!options.RememberLastFolder || !_IsPersistableFolder(fileList.LastOpenedDirectory))
                    {
                        toSave.LastOpenedDirectory = ConfigStore.FileList?.LastOpenedDirectory;
                    }

                    ConfigStore.FileList = toSave;
                }

                if (renameList is not null)
                {
                    ConfigStore.RenameList = renameList;
                }

                ConfigStore.TrySave();
            }
            catch
            {
                // Session save must not block shutdown or surface to the user.
            }
        }

        private static bool _IsPersistableFolder(string? path)
        {
            return FileListPath.IsFilesystemFolderPath(path) && Directory.Exists(path);
        }
    }
}
