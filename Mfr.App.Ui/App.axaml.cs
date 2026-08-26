using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;
using Mfr.Models.Config;

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
        }

        /// <inheritdoc />
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var session = SessionStore.Load();
                var ui = ConfigStore.Config.Ui;
                var initialFolder = ui.RememberLastFolder ? session.LastOpenedDirectory : null;

                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(initialFileListPath: initialFolder),
                };

                UiSessionPersistence.TryRestore(mainWindow, session);
                if (mainWindow.DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.FileListViewModel.ApplySession(FileListSessionSnapshot.FromSessionState(session));
                    viewModel.RenameListViewModel.ApplySession(session.RenameListSortFields);
                }

                desktop.MainWindow = mainWindow;
#if DEBUG
                this.AttachDevTools();
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
