using Avalonia.Controls;
using Mfr.App.Ui.Services.FileList;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Merges UI preferences with in-memory window/folder state into <see cref="ConfigStore"/> sections.
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
            var mainWindow = ConfigStore.MainWindow ?? new SessionStateMainWindow();

            if (mainWindow.RememberWindowState)
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
        /// Merges layout into <see cref="ConfigStore"/> then saves the whole prefs document:
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
            SessionStateFileList? fileList,
            SessionStateRenameList? renameList = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);

            try
            {
                var rememberWindow = (ConfigStore.MainWindow ?? new SessionStateMainWindow()).RememberWindowState;
                var rememberLastFolder = (ConfigStore.FileList ?? new SessionStateFileList()).RememberLastFolder;

                if (rememberWindow)
                {
                    var captured = WindowSession.Capture(window);
                    captured.RememberWindowState = rememberWindow;
                    captured.Splitters = SplitterSession.Capture(panes);
                    ConfigStore.MainWindow = captured;
                }

                if (fileList is not null)
                {
                    var saved = ConfigStore.EnsureFileList();
                    saved.RememberLastFolder = rememberLastFolder;

                    if (rememberLastFolder && _IsPersistableFolder(fileList.LastOpenedDirectory))
                    {
                        saved.LastOpenedDirectory = fileList.LastOpenedDirectory;
                    }

                    saved.FileMask = fileList.FileMask;

                    saved.ExcludeMasks = fileList.ExcludeMasks is null ? null : [.. fileList.ExcludeMasks];

                    saved.ExcludeMasksEnabled = fileList.ExcludeMasksEnabled;

                    saved.MaskSuggestions = fileList.MaskSuggestions is null ? null : [.. fileList.MaskSuggestions];

                    saved.ViewMode = fileList.ViewMode;
                    saved.ThumbnailSize = fileList.ThumbnailSize;
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
