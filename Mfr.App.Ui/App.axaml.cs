using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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

                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(
                        initialFileListPath: initialFolder,
                        persistSession: true,
                        filterDefaults: FilterDefaultsStore.FromConfigStore(),
                        presetManager: PresetManager.OpenDefault()
                    ),
                };

                UiSessionPersistence.TryRestore(mainWindow, mainWindow.GetPaneGrids());

                desktop.MainWindow = mainWindow;
#if DEBUG
                this.AttachDeveloperTools();
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
