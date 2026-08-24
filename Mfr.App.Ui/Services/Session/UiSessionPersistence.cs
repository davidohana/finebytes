using Mfr.App.Ui.Services.FileList;
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

            if (window.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.FileListViewModel.ApplySession(FileListSessionSnapshot.FromSessionState(session));
            }
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

                if (viewModel is not null)
                {
                    var snapshot = viewModel.FileListViewModel.CaptureSession();

                    if (ui.RememberLastFolder && _IsPersistableFolder(snapshot.LastOpenedDirectory))
                    {
                        session.LastOpenedDirectory = snapshot.LastOpenedDirectory;
                    }

                    session.FileMask = snapshot.FileMask;
                    session.ExcludeMasks = snapshot.ExcludeMasks is null ? null : [.. snapshot.ExcludeMasks];
                    session.ExcludeMasksEnabled = snapshot.ExcludeMasksEnabled;
                    session.MaskSuggestions = snapshot.MaskSuggestions is null ? null : [.. snapshot.MaskSuggestions];
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
