using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Models;
using Serilog;

namespace Mfr.App.Ui
{
    /// <summary>
    /// Applies parsed desktop UI startup intents after the main window and pane VMs exist.
    /// </summary>
    internal static class UiStartupArgsApplier
    {
        /// <summary>
        /// Parses <paramref name="args"/> and seeds the Rename List / navigates the File List.
        /// </summary>
        /// <param name="mainWindow">Main window view model (panes already constructed).</param>
        /// <param name="args">Raw desktop argv tokens (may be empty).</param>
        /// <remarks>
        /// Soft-handles parse and apply failures so the UI still opens. Prefer
        /// <see cref="ApplyAsync(MainWindowViewModel, UiStartupArgs, bool)"/> when argv was already parsed
        /// (e.g. App used <c>--initial-folder</c> for the File List ctor path).
        /// </remarks>
        public static Task ApplyAsync(MainWindowViewModel mainWindow, string[] args)
        {
            ArgumentNullException.ThrowIfNull(mainWindow);
            ArgumentNullException.ThrowIfNull(args);

            if (args.Length == 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                return ApplyAsync(mainWindow, UiStartupArgsParser.Parse(args), initialFolderAlreadyApplied: false);
            }
            catch (UserException ex)
            {
                Log.Warning(ex, "Desktop startup arguments could not be parsed.");
                mainWindow.StatusHint = StatusBarText.Warning(ex.Message);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Desktop startup arguments apply failed unexpectedly.");
                mainWindow.StatusHint = StatusBarText.Error(ex.Message);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Seeds the Rename List / navigates the File List from already-parsed startup intents.
        /// </summary>
        /// <param name="mainWindow">Main window view model (panes already constructed).</param>
        /// <param name="startupArgs">Parsed desktop intents.</param>
        /// <param name="initialFolderAlreadyApplied">
        /// When <see langword="true"/>, skips <c>--initial-folder</c> navigate because the File List was
        /// already opened at that path (App ctor). Sources seeding and first-item locate still run.
        /// </param>
        /// <remarks>
        /// Soft-handles apply failures: logs, sets status, and returns without throwing so the UI still
        /// opens (including unexpected exceptions from the fire-and-forget App schedule). Orphan add
        /// modifiers (no sources and no browse folder) are ignored. When sources add at least one item
        /// and <c>--initial-folder</c> was omitted, locates the first added path in the File List.
        /// </remarks>
        public static async Task ApplyAsync(
            MainWindowViewModel mainWindow,
            UiStartupArgs startupArgs,
            bool initialFolderAlreadyApplied = false
        )
        {
            ArgumentNullException.ThrowIfNull(mainWindow);
            ArgumentNullException.ThrowIfNull(startupArgs);

            try
            {
                await _ApplyParsedAsync(mainWindow, startupArgs, initialFolderAlreadyApplied).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // Fire-and-forget from App must not fault the UI thread dispatcher.
                Log.Error(ex, "Desktop startup arguments apply failed unexpectedly.");
                mainWindow.StatusHint = StatusBarText.Error(ex.Message);
            }
        }

        /// <summary>
        /// Applies Rename List / File List intents (soft-fail per step).
        /// </summary>
        /// <param name="mainWindow">Main window view model (panes already constructed).</param>
        /// <param name="startupArgs">Parsed desktop intents.</param>
        /// <param name="initialFolderAlreadyApplied">Skip browse navigate when the ctor already opened it.</param>
        private static async Task _ApplyParsedAsync(
            MainWindowViewModel mainWindow,
            UiStartupArgs startupArgs,
            bool initialFolderAlreadyApplied
        )
        {
            // Orphan add modifiers (no sources and no browse folder) are ignored after parse.
            if (startupArgs.Sources.Count == 0 && startupArgs.InitialFolder is null)
            {
                return;
            }

            string? firstAddedPath = null;
            if (startupArgs.Sources.Count > 0)
            {
                try
                {
                    firstAddedPath = await mainWindow
                        .RenameListViewModel.AddStartupSourcesAsync(
                            sources: startupArgs.Sources,
                            includeFiles: startupArgs.IncludeFiles,
                            includeFolders: startupArgs.IncludeFolders,
                            includeSubdirs: startupArgs.IncludeSubdirs,
                            includeHidden: startupArgs.IncludeHidden
                        )
                        .ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Desktop startup failed while seeding the Rename List.");
                    mainWindow.StatusHint = StatusBarText.Error(ex.Message);
                }
            }

            if (startupArgs.InitialFolder is not null)
            {
                if (!initialFolderAlreadyApplied)
                {
                    // NavigateTo soft-fails into File List status; it does not throw.
                    mainWindow.FileListViewModel.NavigateTo(startupArgs.InitialFolder);
                }

                return;
            }

            if (firstAddedPath is null)
            {
                return;
            }

            // TryLocatePath soft-fails (returns false); it does not throw.
            _ = mainWindow.FileListViewModel.TryLocatePath(firstAddedPath);
        }
    }
}
