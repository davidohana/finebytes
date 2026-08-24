using Avalonia.Controls;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Main application window with the MFR 7.4 splitter layout.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += _OnClosing;
        }

        private void _OnClosing(object? sender, WindowClosingEventArgs e)
        {
            var fileListSnapshot = (DataContext as MainWindowViewModel)?.FileListViewModel.CaptureSession();
            UiSessionPersistence.SaveOnClose(this, fileListSnapshot);
        }
    }
}
