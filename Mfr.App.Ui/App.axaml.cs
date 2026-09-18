using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.Views.GridColumnSizing;
using Mfr.App.Ui.Views.MainWindow;
using Mfr.Engine.Config;
using Mfr.Engine.Presets;

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
                var initialFolder = ConfigStore.Options.RememberLastFolder
                    ? ConfigStore.FileList?.LastOpenedDirectory
                    : null;

                var mainWindowViewModel = new MainWindowViewModel(
                    initialFileListPath: initialFolder,
                    persistSession: true,
                    filterDefaults: FilterDefaultsStore.FromConfigStore(),
                    presetManager: PresetManager.OpenDefault()
                );
                var mainWindow = new MainWindow { DataContext = mainWindowViewModel };

                UiSessionPersistence.TryRestore(mainWindow, mainWindow.GetPaneGrids());

                desktop.MainWindow = mainWindow;
                _ScheduleStartupArgsApply(mainWindowViewModel, desktop.Args);
#if DEBUG
                this.AttachDeveloperTools();
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Posts desktop argv apply after the main window is assigned so the UI still opens on failure.
        /// </summary>
        /// <param name="mainWindowViewModel">Root view model with File List and Rename List panes.</param>
        /// <param name="args">Desktop argv from Avalonia (may be null or empty).</param>
        private static void _ScheduleStartupArgsApply(MainWindowViewModel mainWindowViewModel, string[]? args)
        {
            if (args is null || args.Length == 0)
            {
                return;
            }

            Dispatcher.UIThread.Post(() => _ = UiStartupArgsApplier.ApplyAsync(mainWindowViewModel, args));
        }
    }
}
