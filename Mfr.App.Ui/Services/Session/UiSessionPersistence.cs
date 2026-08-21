using Mfr.App.Ui.ViewModels;
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
        /// Restores remembered main-window layout sections from <paramref name="session"/>.
        /// </summary>
        /// <param name="window">Main window to configure.</param>
        /// <param name="session">Loaded session document.</param>
        public static void TryRestore(MainWindow window, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(session);

            if (!ConfigStore.Config.Ui.RememberWindowState)
                return;

            WindowSession.TryRestore(window, session.Window);
            SplitterSession.TryRestore(window, session.Splitters);
        }

        /// <summary>
        /// Updates <c>session.json</c> for enabled remember flags; leaves disabled fields unchanged.
        /// </summary>
        /// <param name="window">Main window providing layout to capture.</param>
        /// <param name="viewModel">Root VM providing the File List path.</param>
        public static void SaveOnClose(MainWindow window, MainWindowViewModel? viewModel)
        {
            ArgumentNullException.ThrowIfNull(window);

            var ui = ConfigStore.Config.Ui;
            if (!ui.RememberWindowState && !ui.RememberLastFolder)
                return;

            try
            {
                var session = SessionStore.Load();

                if (ui.RememberWindowState)
                {
                    session.Window = WindowSession.Capture(window);
                    session.Splitters = SplitterSession.Capture(window);
                }

                if (ui.RememberLastFolder && viewModel is not null)
                {
                    var path = viewModel.FileList.CurrentPath;
                    if (_IsPersistableFolder(path))
                        session.LastOpenedDirectory = path;
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
                return false;

            if (FileListPath.IsComputerPath(path) || FileListPath.IsNetworkPath(path))
                return false;

            return Directory.Exists(path);
        }
    }
}
