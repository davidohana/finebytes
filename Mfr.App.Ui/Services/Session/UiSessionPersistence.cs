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
            FileListPrefs? fileList,
            RenameListPrefs? renameList = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);

            try
            {
                var rememberWindow = ConfigStore.Options.RememberWindowState;
                var rememberLastFolder = ConfigStore.FileList?.RememberLastFolder ?? true;

                if (rememberWindow)
                {
                    var captured = WindowSession.Capture(window);
                    captured.Splitters = SplitterSession.Capture(panes);
                    ConfigStore.MainWindow = captured;
                }

                if (fileList is not null)
                {
                    // Merge layout fields only — Options-owned prefs (e.g. DoubleClickAddsToRenameList) stay.
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
                    // Merge pane fields only — Options-owned add policy stays on ConfigStore.
                    var saved = ConfigStore.EnsureRenameList();
                    saved.SortFields = renameList.SortFields is null ? null : [.. renameList.SortFields];
                    saved.VisibleColumns = renameList.VisibleColumns is null ? null : [.. renameList.VisibleColumns];
                    saved.UseFixedWidthFont = renameList.UseFixedWidthFont;
                    saved.PreviewEnabled = renameList.PreviewEnabled;
                    saved.AbModeEnabled = renameList.AbModeEnabled;
                    saved.AbSide = RenameListPrefs.NormalizeAbSide(renameList.AbSide);
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
