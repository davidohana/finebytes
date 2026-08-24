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
        /// File List mask fields are restored separately via
        /// <see cref="FileListSessionSnapshot.FromSessionState"/> and the File List view model.
        /// </remarks>
        public static void TryRestore(MainWindow window, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(session);

            var windowRestored = false;
            if (ConfigStore.Config.Ui.RememberWindowState)
            {
                windowRestored = WindowSession.TryRestore(window, session.Window);
                SplitterSession.TryRestore(window, session.Splitters);
            }

            if (!windowRestored)
            {
                WindowSession.ApplyDefault(window);
            }
        }

        /// <summary>
        /// Updates <c>session.json</c> for enabled remember flags; leaves disabled fields unchanged.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="fileListSnapshot">
        /// File List mask and folder fields to persist, or <see langword="null"/> when unavailable.
        /// </param>
        public static void SaveOnClose(MainWindow window, FileListSessionSnapshot? fileListSnapshot)
        {
            ArgumentNullException.ThrowIfNull(window);

            var ui = ConfigStore.Config.Ui;
            if (!ui.RememberWindowState && !ui.RememberLastFolder)
            {
                return;
            }

            try
            {
                var session = SessionStore.Load();

                if (ui.RememberWindowState)
                {
                    session.Window = WindowSession.Capture(window);
                    session.Splitters = SplitterSession.Capture(window);
                }

                if (fileListSnapshot is not null)
                {
                    if (ui.RememberLastFolder && _IsPersistableFolder(fileListSnapshot.LastOpenedDirectory))
                    {
                        session.LastOpenedDirectory = fileListSnapshot.LastOpenedDirectory;
                    }

                    session.FileMask = fileListSnapshot.FileMask;
                    session.ExcludeMasks = fileListSnapshot.ExcludeMasks is null
                        ? null
                        : [.. fileListSnapshot.ExcludeMasks];
                    session.ExcludeMasksEnabled = fileListSnapshot.ExcludeMasksEnabled;
                    session.MaskSuggestions = fileListSnapshot.MaskSuggestions is null
                        ? null
                        : [.. fileListSnapshot.MaskSuggestions];
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
