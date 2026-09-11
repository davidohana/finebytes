using Avalonia.Controls;
using Mfr.App.Ui.Services.FileList;
using Mfr.Engine.Presets;
using Mfr.Models.Config;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Merges UI preferences with in-memory window/folder state into a <see cref="SessionState"/>.
    /// </summary>
    internal static class UiSessionPersistence
    {
        /// <summary>
        /// Restores remembered main-window layout from <paramref name="session"/>.
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="panes">Pane grids for splitter restore.</param>
        /// <param name="session">Loaded session document.</param>
        /// <remarks>
        /// File List mask fields and Rename List session fields are restored separately via
        /// <see cref="FileListSessionSnapshot.FromSessionState"/> and pane apply/capture methods.
        /// </remarks>
        public static void TryRestore(Window window, MainWindowPaneGrids panes, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);
            ArgumentNullException.ThrowIfNull(session);

            var windowRestored = false;
            var mainWindow = session.MainWindow ?? new SessionStateMainWindow();

            if (mainWindow.RememberWindowState)
            {
                windowRestored = WindowSession.TryRestore(window, session.MainWindow);

                SplitterSession.TryRestore(panes, session.MainWindow?.Splitters);
            }

            if (!windowRestored)
            {
                WindowSession.ApplyDefault(window);
            }
        }

        /// <summary>
        /// Updates <c>session.json</c>: window/folder when their remember flags are on; masks, Rename List,
        /// and Applied Filters chain always.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="panes">Pane grids for splitter capture.</param>
        /// <param name="session">Live session document to merge into and write.</param>
        /// <param name="fileListSnapshot">
        /// File List mask and folder fields to persist, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="renameList">
        /// Rename List session fields, or <see langword="null"/> to leave the saved section unchanged.
        /// </param>
        /// <param name="appliedFilters">
        /// Working Applied Filters chain, or <see langword="null"/> to leave the saved chain unchanged.
        /// </param>
        /// <param name="sessionFilePath">
        /// Path to <c>session.json</c>. When <c>null</c> or whitespace, the default AppData file is used.
        /// </param>
        public static void SaveOnClose(
            Window window,
            MainWindowPaneGrids panes,
            SessionState session,
            FileListSessionSnapshot? fileListSnapshot,
            SessionStateRenameList? renameList = null,
            FilterChain? appliedFilters = null,
            string? sessionFilePath = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(panes);
            ArgumentNullException.ThrowIfNull(session);

            try
            {
                var rememberWindow = _RememberWindowState(session);
                var rememberLastFolder = _RememberLastFolder(session);

                if (rememberWindow)
                {
                    var captured = WindowSession.Capture(window);
                    captured.RememberWindowState = rememberWindow;
                    captured.Splitters = SplitterSession.Capture(panes);
                    session.MainWindow = captured;
                }

                if (fileListSnapshot is not null)
                {
                    var fileList = session.EnsureFileList();
                    fileList.RememberLastFolder = rememberLastFolder;

                    if (rememberLastFolder && _IsPersistableFolder(fileListSnapshot.LastOpenedDirectory))
                    {
                        fileList.LastOpenedDirectory = fileListSnapshot.LastOpenedDirectory;
                    }

                    fileList.FileMask = fileListSnapshot.FileMask;

                    fileList.ExcludeMasks = fileListSnapshot.ExcludeMasks is null
                        ? null
                        : [.. fileListSnapshot.ExcludeMasks];

                    fileList.ExcludeMasksEnabled = fileListSnapshot.ExcludeMasksEnabled;

                    fileList.MaskSuggestions = fileListSnapshot.MaskSuggestions is null
                        ? null
                        : [.. fileListSnapshot.MaskSuggestions];
                }

                if (renameList is not null)
                {
                    session.RenameList = renameList;
                }

                if (appliedFilters is not null)
                {
                    session.AppliedFilters = appliedFilters;
                }

                SessionStore.Save(session, sessionFilePath, SessionJsonOptions.Default);
            }
            catch
            {
                // Session save must not block shutdown or surface to the user.
            }
        }

        private static bool _RememberWindowState(SessionState session)
        {
            return (session.MainWindow ?? new SessionStateMainWindow()).RememberWindowState;
        }

        private static bool _RememberLastFolder(SessionState session)
        {
            return (session.FileList ?? new SessionStateFileList()).RememberLastFolder;
        }

        private static bool _IsPersistableFolder(string? path)
        {
            return FileListPath.IsFilesystemFolderPath(path) && Directory.Exists(path);
        }
    }
}
