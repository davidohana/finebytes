using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.Utils;

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
            PathCombo.PropertyChanged += _OnPathComboPropertyChanged;
        }

        private void _OnPathComboPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != IsVisibleProperty || !PathCombo.IsVisible)
                return;

            Dispatcher.UIThread.Post(() => PathCombo.Focus());
        }

        private void _OnAddressBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel || viewModel.IsPathEditing)
                return;

            if (e.Source is Visual visual && visual.FindAncestorOfType<Button>(includeSelf: true) is not null)
                return;

            viewModel.BeginPathEdit();
            e.Handled = true;
        }

        private void _OnHistoryClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel)
                return;

            viewModel.BeginPathEdit();
            Dispatcher.UIThread.Post(() =>
            {
                PathCombo.Focus();
                PathCombo.IsDropDownOpen = true;
            });
        }

        private void _OnPathKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel)
                return;

            if (e.Key == Key.Escape)
            {
                viewModel.CancelPathEdit();
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Enter)
                return;

            viewModel.CommitPath();
            e.Handled = true;
        }

        private void _OnPathLostFocus(object? sender, RoutedEventArgs e)
        {
            if (PathCombo.IsDropDownOpen)
                return;

            if (DataContext is FileListViewModel viewModel)
                viewModel.CommitPath();
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

        private void _OnReportGridLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid && DataContext is FileListViewModel viewModel)
                _SyncReportSortGlyphs(grid, viewModel);
        }

        private void _OnEntriesSorting(object? sender, DataGridColumnEventArgs e)
        {
            e.Handled = true;
            if (sender is not DataGrid grid || DataContext is not FileListViewModel viewModel)
                return;

            viewModel.SortByColumn(e.Column.SortMemberPath);
            _SyncReportSortGlyphs(grid, viewModel);
        }

        private static void _SyncReportSortGlyphs(DataGrid grid, FileListViewModel viewModel)
        {
            var view = grid.CollectionView;
            if (view?.SortDescriptions is null)
                return;

            var direction = viewModel.IsSortAscending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(
                    DataGridSortDescription.FromPath(
                        nameof(FileListEntry.IsDirectory),
                        ListSortDirection.Descending));
                view.SortDescriptions.Add(_CreateColumnSort(viewModel.SortMemberPath, direction));
            }
        }

        private static DataGridSortDescription _CreateColumnSort(string memberPath, ListSortDirection direction)
        {
            var isStringColumn = memberPath is nameof(FileListEntry.Name) or nameof(FileListEntry.Type);
            if (isStringColumn)
                return DataGridSortDescription.FromPath(memberPath, direction, PathComparers.Os);

            return DataGridSortDescription.FromPath(memberPath, direction);
        }
    }
}
