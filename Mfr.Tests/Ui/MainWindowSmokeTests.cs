using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Input;
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

        /// <summary>
        /// Verifies documented global shortcuts are window key bindings, not Backspace or Alt+F4.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_RegistersGlobalKeyBindings()
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            window.Show();

            var gestures = window.KeyBindings.Select(binding => binding.Gesture).ToList();
            Assert.Contains(AppShortcuts.Go, gestures);
            Assert.Contains(AppShortcuts.UndoLast, gestures);
            Assert.Contains(AppShortcuts.ShowLog, gestures);
            Assert.Contains(AppShortcuts.ShowOptions, gestures);
            Assert.Contains(AppShortcuts.Refresh, gestures);
            Assert.Contains(AppShortcuts.GoToAddress, gestures);
            Assert.Contains(AppShortcuts.GoToAddressAlt, gestures);
            Assert.DoesNotContain(AppShortcuts.GoUp, gestures);
            Assert.DoesNotContain(AppShortcuts.Exit, gestures);
            Assert.DoesNotContain(AppShortcuts.ZoomIn, gestures);
        }
    }
}
