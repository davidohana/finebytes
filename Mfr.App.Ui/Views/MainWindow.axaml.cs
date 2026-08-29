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
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var session = viewModel.Session;
            if (session is null)
            {
                return;
            }

            UiSessionPersistence.SaveOnClose(
                this,
                session,
                viewModel.FileListViewModel.CaptureSession(),
                viewModel.RenameListViewModel.CaptureSession()
            );
        }
    }
}
