using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
            viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            Closed += (_, _) => viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
        }

        private RenameListFieldShuttleDialogViewModel? _ViewModel =>
            DataContext as RenameListFieldShuttleDialogViewModel;

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName
                is not (
                    nameof(RenameListFieldShuttleDialogViewModel.SelectedColumnRows)
                    or nameof(RenameListFieldShuttleDialogViewModel.SelectedSortRows)
                )
            )
            {
                return;
            }

            Dispatcher.UIThread.Post(_RestoreSelectedListIndexes, DispatcherPriority.Background);
        }

        private void _RestoreSelectedListIndexes()
        {
            if (_ViewModel is null)
            {
                return;
            }

            _RestoreListIndex(SelectedColumnsList, _ViewModel.SelectedColumnRowIndex);
            _RestoreListIndex(SelectedSortList, _ViewModel.SelectedSortRowIndex);
        }

        private static void _RestoreListIndex(ListBox? list, int selectedIndex)
        {
            if (list is null || list.SelectedIndex == selectedIndex)
            {
                return;
            }

            list.SelectedIndex = selectedIndex;
        }

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
