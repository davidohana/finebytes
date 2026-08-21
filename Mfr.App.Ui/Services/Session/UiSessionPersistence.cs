using Mfr.App.Ui.ViewModels;
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
        /// Updates <c>session.json</c> for enabled remember flags; leaves disabled fields unchanged.
        /// </summary>
        /// <param name="viewModel">Root VM providing the File List path.</param>
        /// <param name="windowGeometry">Captured main-window geometry when remembering window state.</param>
        public static void SaveOnClose(MainWindowViewModel? viewModel, SessionWindowState? windowGeometry)
        {
            var ui = ConfigLoader.Settings.Ui;
            if (!ui.RememberWindowState && !ui.RememberLastFolder)
                return;

            try
            {
                var session = SessionStore.Load();

                if (ui.RememberWindowState && windowGeometry is not null)
                    session.Window = windowGeometry;

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
