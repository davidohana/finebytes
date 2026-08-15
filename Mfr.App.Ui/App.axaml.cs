using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
#if DEBUG
                this.AttachDevTools();
#endif
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
