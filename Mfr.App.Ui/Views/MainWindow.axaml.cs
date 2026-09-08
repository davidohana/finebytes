using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
            DataContextChanged += _OnDataContextChanged;
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            viewModel.AppliedFiltersViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;
            viewModel.AppliedFiltersViewModel.FilterDefaultSaved += _OnFilterDefaultSaved;
        }

        private void _OnFilterDefaultSaved(object? sender, string catalogDisplayName)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowFilterDefaultSavedAsync(catalogDisplayName));
        }

        private async Task _ShowFilterDefaultSavedAsync(string catalogDisplayName)
        {
            var dialog = new OkMessageDialog(
                title: "Magic File Renamer",
                message: $"Filter settings for the filter '{catalogDisplayName}' saved as default."
            );
            await dialog.ShowDialog(this);
        }

        private void _OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            viewModel.AppliedFiltersViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;

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

        private async void _OnFilterOptionsMenuClick(object? sender, RoutedEventArgs e)
        {
            await AppliedFiltersPane.ShowFilterOptionsAsync();
        }
    }
}
