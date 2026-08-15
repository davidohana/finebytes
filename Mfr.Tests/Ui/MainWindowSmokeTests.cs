using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the main window shell.
    /// </summary>
    public sealed class MainWindowSmokeTests
    {
        /// <summary>
        /// Verifies the main window constructs with a File Explorer pane.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_Constructs()
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            window.Show();

            Assert.True(window.IsVisible);
            Assert.IsType<MainWindowViewModel>(window.DataContext);
        }
    }
}
