using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// File Explorer pane host.
    /// </summary>
    public partial class FileListView : UserControl
    {
        /// <summary>
        /// Initializes the File Explorer pane.
        /// </summary>
        public FileListView()
        {
            InitializeComponent();
        }

        private void _OnPathKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            _CommitPath();
            e.Handled = true;
        }

        private void _OnPathLostFocus(object? sender, RoutedEventArgs e)
        {
            _CommitPath();
        }

        private void _OnEntriesDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is FileListViewModel viewModel)
                viewModel.OpenSelected();
        }

        private void _OnEntriesKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back)
                return;

            if (DataContext is FileListViewModel viewModel)
                viewModel.GoUp();

            e.Handled = true;
        }

        private void _CommitPath()
        {
            if (DataContext is FileListViewModel viewModel)
                viewModel.CommitPath();
        }
    }
}
