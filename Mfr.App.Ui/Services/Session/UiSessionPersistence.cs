using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Views;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Merges UI preferences with in-memory window/folder state into <see cref="SessionStore"/>.
    /// </summary>
    internal static class UiSessionPersistence
    {
        /// <summary>
        /// Restores remembered main-window layout from <paramref name="session"/>.
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="session">Loaded session document.</param>
        /// <remarks>
        /// File List mask fields and Rename List Auto-Sort are restored separately via
        /// <see cref="FileListSessionSnapshot.FromSessionState"/> / <c>ApplySession</c>.
        /// </remarks>
        public static void TryRestore(MainWindow window, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(window);

            ArgumentNullException.ThrowIfNull(session);

            var windowRestored = false;

            if (ConfigStore.Config.Ui.RememberWindowState)
            {
                windowRestored = WindowSession.TryRestore(window, session.MainWindow);

                SplitterSession.TryRestore(window, session.MainWindow?.Splitters);
            }

            if (!windowRestored)
            {
                WindowSession.ApplyDefault(window);
            }
        }

        /// <summary>
        /// Updates <c>session.json</c>: window/folder when their remember flags are on; masks and Auto-Sort always.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="fileListSnapshot">
        /// File List mask and folder fields to persist, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="renameListSortFields">
        /// Encoded Rename List Auto-Sort keys, or <see langword="null"/> to leave the saved value unchanged.
        /// </param>
        public static void SaveOnClose(
            MainWindow window,
            FileListSessionSnapshot? fileListSnapshot,
            string? renameListSortFields = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);

            try
            {
                var session = SessionStore.Load();

                var ui = ConfigStore.Config.Ui;

                if (ui.RememberWindowState)
                {
                    session.MainWindow = WindowSession.Capture(window);

                    session.MainWindow.Splitters = SplitterSession.Capture(window);
                }

                if (fileListSnapshot is not null)
                {
                    session.FileList ??= new SessionStateFileList();

                    var fileList = session.FileList;

                    if (ui.RememberLastFolder && _IsPersistableFolder(fileListSnapshot.LastOpenedDirectory))
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

                if (renameListSortFields is not null)
                {
                    session.RenameList ??= new SessionStateRenameList();

                    session.RenameList.SortFields = renameListSortFields;
                }

                SessionStore.Save(session);
            }
            catch
            {
                // Session save must not block shutdown or surface to the user.
            }
        }

        private static bool _IsPersistableFolder(string? path)
        {
            if (path.IsBlank())
            {
                return false;
            }

            if (FileListPath.IsComputerPath(path) || FileListPath.IsNetworkPath(path))
            {
                return false;
            }

            return Directory.Exists(path);
        }
    }
}
