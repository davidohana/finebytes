using Avalonia.Controls;
using Mfr.App.Ui.ViewModels;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Merges UI preferences with in-memory window/folder state into <see cref="SessionStore"/>.
    /// </summary>
    internal static class UiSessionPersistence
    {
        /// <summary>
        /// Updates <c>session.json</c> for enabled remember flags; leaves disabled fields unchanged.
        /// </summary>
        /// <param name="window">Main window (for geometry capture).</param>
        /// <param name="viewModel">Root VM providing the explorer path.</param>
        /// <param name="hasNormalBounds">True when normal-state bounds were observed.</param>
        /// <param name="normalX">Tracked normal-state X.</param>
        /// <param name="normalY">Tracked normal-state Y.</param>
        /// <param name="normalWidth">Tracked normal-state width.</param>
        /// <param name="normalHeight">Tracked normal-state height.</param>
        public static void SaveOnClose(
            Window window,
            MainWindowViewModel? viewModel,
            bool hasNormalBounds,
            int normalX,
            int normalY,
            double normalWidth,
            double normalHeight)
        {
            var ui = ConfigLoader.Settings.Ui;
            if (!ui.RememberWindowState && !ui.RememberLastFolder)
                return;

            try
            {
                var session = SessionStore.Load();

                if (ui.RememberWindowState)
                {
                    session.Window = WindowSession.Capture(
                        window,
                        hasNormalBounds,
                        normalX,
                        normalY,
                        normalWidth,
                        normalHeight);
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

            if (ExplorerPath.IsComputerPath(path) || ExplorerPath.IsNetworkPath(path))
                return false;

            return Directory.Exists(path);
        }
    }
}
