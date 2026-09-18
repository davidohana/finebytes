using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.Views.GridColumnSizing;
using Mfr.App.Ui.Views.MainWindow;
using Mfr.Engine.Config;
using Mfr.Engine.Presets;
using Mfr.Models;
using Serilog;

namespace Mfr.App.Ui
{
    /// <summary>
    /// Avalonia application host for the Magic File Renamer GUI.
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            AppChromeFonts.AddResources(Resources);
        }

        /// <inheritdoc />
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var args = desktop.Args ?? [];
                var (startupArgs, parseWarning) = _TryParseStartupArgs(args);
                var initialFolder = startupArgs.InitialFolder ?? _RememberedFileListFolder();

                var mainWindowViewModel = new MainWindowViewModel(
                    initialFileListPath: initialFolder,
                    persistSession: true,
                    filterDefaults: FilterDefaultsStore.FromConfigStore(),
                    presetManager: PresetManager.OpenDefault()
                );
                var mainWindow = new MainWindow { DataContext = mainWindowViewModel };

                UiSessionPersistence.TryRestore(mainWindow, mainWindow.GetPaneGrids());

                desktop.MainWindow = mainWindow;
                _ScheduleStartupArgsApply(mainWindowViewModel, startupArgs, parseWarning);
#if DEBUG
                this.AttachDeveloperTools();
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Parses desktop argv once before the main window is built.
        /// </summary>
        /// <param name="args">Desktop argv from Avalonia (may be empty).</param>
        /// <returns>Parsed intents (empty on failure) and an optional soft-fail warning message.</returns>
        private static (UiStartupArgs StartupArgs, string? ParseWarning) _TryParseStartupArgs(string[] args)
        {
            if (args.Length == 0)
            {
                return (UiStartupArgs.Empty, null);
            }

            try
            {
                return (UiStartupArgsParser.Parse(args), null);
            }
            catch (UserException ex)
            {
                Log.Warning(ex, "Desktop startup arguments could not be parsed.");
                return (UiStartupArgs.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Options-owned last File List folder when remember-last is enabled.
        /// </summary>
        /// <returns>Last opened directory, or <c>null</c> when remember-last is off or unset.</returns>
        private static string? _RememberedFileListFolder()
        {
            return ConfigStore.Options.RememberLastFolder ? ConfigStore.FileList?.LastOpenedDirectory : null;
        }

        /// <summary>
        /// Posts remaining startup apply after the main window is assigned so the UI still opens on failure.
        /// </summary>
        /// <param name="mainWindowViewModel">Root view model with File List and Rename List panes.</param>
        /// <param name="startupArgs">Already-parsed desktop intents (<c>--initial-folder</c> already used for ctor).</param>
        /// <param name="parseWarning">Soft-fail parse message when argv was invalid; otherwise <c>null</c>.</param>
        private static void _ScheduleStartupArgsApply(
            MainWindowViewModel mainWindowViewModel,
            UiStartupArgs startupArgs,
            string? parseWarning
        )
        {
            if (parseWarning is not null)
            {
                Dispatcher.UIThread.Post(() => mainWindowViewModel.StatusHint = StatusBarText.Warning(parseWarning));
                return;
            }

            if (startupArgs.Sources.Count == 0 && startupArgs.InitialFolder is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
                _ = UiStartupArgsApplier.ApplyAsync(mainWindowViewModel, startupArgs, initialFolderAlreadyApplied: true)
            );
        }
    }
}
