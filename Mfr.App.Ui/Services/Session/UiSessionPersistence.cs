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

            var viewModel = window.DataContext as MainWindowViewModel;
            if (viewModel is not null)
            {
                var fileListViewModel = viewModel.FileListViewModel;
                if (!string.IsNullOrEmpty(session.FileMask))
                {
                    fileListViewModel.Mask = session.FileMask;
                }

                // Null means unset: keep the defaults. An empty list means the user cleared them.
                if (session.ExcludeMasks is not null)
                {
                    fileListViewModel.ExcludeMasks = [.. session.ExcludeMasks];
                }

                if (session.ExcludeMasksEnabled is { } excludeEnabled)
                {
                    fileListViewModel.ExcludeMasksEnabled = excludeEnabled;
                }

                if (session.MaskSuggestions is { Count: > 0 })
                {
                    fileListViewModel.MaskSuggestions.Clear();
                    foreach (var mask in session.MaskSuggestions)
                    {
                        fileListViewModel.MaskSuggestions.Add(mask);
                    }
                }
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
                    var fileListViewModel = viewModel.FileListViewModel;

                    if (ui.RememberLastFolder)
                    {
                        var path = fileListViewModel.CurrentPath;
                        if (_IsPersistableFolder(path))
                        {
                            session.LastOpenedDirectory = path;
                        }
                    }

                    session.FileMask = fileListViewModel.Mask;
                    session.ExcludeMasks = [.. fileListViewModel.ExcludeMasks];
                    session.ExcludeMasksEnabled = fileListViewModel.ExcludeMasksEnabled;
                    session.MaskSuggestions = [.. fileListViewModel.MaskSuggestions];
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
