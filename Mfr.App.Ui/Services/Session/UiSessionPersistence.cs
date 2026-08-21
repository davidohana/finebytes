using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FileList;
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
                var fileList = viewModel.FileList;
                if (!string.IsNullOrEmpty(session.FileMask))
                {
                    fileList.Mask = session.FileMask;
                }

                // Blank means "unset" (older sessions saved "" from the inline Exclude box).
                // Keep the MFR 7 defaults (*.exe;*.dll;*.sys) unless the user stored real masks.
                if (!string.IsNullOrWhiteSpace(session.ExcludeMasks))
                {
                    fileList.ExcludeMasks = session.ExcludeMasks;
                }

                if (session.ExcludeMasksEnabled is { } excludeEnabled)
                {
                    fileList.ExcludeMasksEnabled = excludeEnabled;
                }

                if (session.MaskSuggestions is { Count: > 0 })
                {
                    fileList.MaskSuggestions.Clear();
                    foreach (var mask in session.MaskSuggestions)
                    {
                        fileList.MaskSuggestions.Add(mask);
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
                    var fileList = viewModel.FileList;

                    if (ui.RememberLastFolder)
                    {
                        var path = fileList.CurrentPath;
                        if (_IsPersistableFolder(path))
                        {
                            session.LastOpenedDirectory = path;
                        }
                    }

                    session.FileMask = fileList.Mask;
                    session.ExcludeMasks = fileList.ExcludeMasks;
                    session.ExcludeMasksEnabled = fileList.ExcludeMasksEnabled;
                    session.MaskSuggestions = [.. fileList.MaskSuggestions];
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
