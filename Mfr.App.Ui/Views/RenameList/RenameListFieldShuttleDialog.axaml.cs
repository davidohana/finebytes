using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List visible columns and Auto-Sort keys.
    /// </summary>
    public partial class RenameListFieldShuttleDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public RenameListFieldShuttleDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft column layout and sort keys.</param>
        public RenameListFieldShuttleDialog(RenameListFieldShuttleDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private RenameListFieldShuttleDialogViewModel? _ViewModel =>
            DataContext as RenameListFieldShuttleDialogViewModel;

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel is { CanConfirm: false })
            {
                return;
            }

            Close(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void _OnAddOriginalDoubleTapped(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.AddSelectedOriginalFieldCommand.Execute(null);
        }

        private void _OnAddPreviewDoubleTapped(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.AddSelectedPreviewFieldCommand.Execute(null);
        }

        private void _OnAddSortDoubleTapped(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.AddSelectedSortFieldCommand.Execute(null);
        }

        private void _OnRemoveColumnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.RemoveSelectedColumnCommand.Execute(null);
        }

        private void _OnRemoveSortDoubleTapped(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.RemoveSelectedSortKeyCommand.Execute(null);
        }

        private void _OnAddSelectedClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel is null)
            {
                return;
            }

            if (_ViewModel.IsPreviewColumnsTab)
            {
                _ViewModel.AddSelectedPreviewFieldCommand.Execute(null);
                return;
            }

            _ViewModel.AddSelectedOriginalFieldCommand.Execute(null);
        }

        private void _OnAddAllClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel is null)
            {
                return;
            }

            if (_ViewModel.IsPreviewColumnsTab)
            {
                _ViewModel.AddAllPreviewFieldsCommand.Execute(null);
                return;
            }

            _ViewModel.AddAllOriginalFieldsCommand.Execute(null);
        }

        private void _OnAddSortClick(object? sender, RoutedEventArgs e)
        {
            _ViewModel?.AddSelectedSortFieldCommand.Execute(null);
        }

        private void _OnToggleSortDirectionClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel is null || sender is not Button button)
            {
                return;
            }

            if (button.DataContext is RenameListFieldShuttleSortRow row)
            {
                _ViewModel.SelectedSortRowIndex = row.Index;
            }

            _ViewModel.ToggleSelectedSortDirectionCommand.Execute(null);
        }
    }
}
