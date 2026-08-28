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
            var viewModel = DataContext as MainWindowViewModel;
            var fileListSnapshot = viewModel?.FileListViewModel.CaptureSession();
            var renameListSortFields = viewModel?.RenameListViewModel.CaptureSortFields();
            var renameListVisibleColumns = viewModel?.RenameListViewModel.CaptureVisibleColumnsForSession();
            UiSessionPersistence.SaveOnClose(this, fileListSnapshot, renameListSortFields, renameListVisibleColumns);
        }
    }
}
