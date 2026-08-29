using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Views;
using Mfr.Models.Config;
using Mfr.Utils;

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
        /// <param name="session">Loaded session document.</param>
        /// <remarks>
        /// File List mask fields and Rename List session fields are restored separately via
        /// <see cref="FileListSessionSnapshot.FromSessionState"/> and pane apply/capture methods.
        /// </remarks>
        public static void TryRestore(MainWindow window, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(window);

            ArgumentNullException.ThrowIfNull(session);

            var windowRestored = false;

            if (session.MainWindow?.RememberWindowState ?? true)
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
        /// Updates <c>session.json</c>: window/folder when their remember flags are on; masks and Rename List always.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="session">Live session document to merge into and write.</param>
        /// <param name="fileListSnapshot">
        /// File List mask and folder fields to persist, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="renameList">
        /// Rename List session fields, or <see langword="null"/> to leave the saved section unchanged.
        /// </param>
        public static void SaveOnClose(
            MainWindow window,
            SessionState session,
            FileListSessionSnapshot? fileListSnapshot,
            SessionStateRenameList? renameList = null
        )
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(session);

            try
            {
                var rememberWindow = session.MainWindow?.RememberWindowState ?? true;
                var rememberLastFolder = session.FileList?.RememberLastFolder ?? true;

                if (rememberWindow)
                {
                    var captured = WindowSession.Capture(window);
                    captured.RememberWindowState = rememberWindow;
                    captured.Splitters = SplitterSession.Capture(window);
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
