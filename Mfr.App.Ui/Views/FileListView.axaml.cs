using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.Utils;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// File List pane host.
    /// </summary>
    public partial class FileListView : UserControl
    {
        /// <summary>
        /// Initializes the File List pane.
        /// </summary>
        public FileListView()
        {
            InitializeComponent();
            ThumbnailsList.AddHandler(
                PointerWheelChangedEvent,
                _OnThumbnailsPointerWheelChanged,
                RoutingStrategies.Tunnel
            );
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_TryHandleThumbnailZoomKeys(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void _OnMaskKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (DataContext is FileListViewModel viewModel)
            {
                viewModel.CommitMask();
            }

            e.Handled = true;
        }

        private void _OnMaskLostFocus(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(_CommitMaskIfInactive, DispatcherPriority.Input);
        }

        private async void _OnExcludeMasksClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialogVm = new ExcludeMasksDialogViewModel(viewModel.ExcludeMasksEnabled, viewModel.ExcludeMasks);
            var dialog = new ExcludeMasksDialog(dialogVm);
            var accepted = await dialog.ShowDialog<bool?>(owner);
            if (accepted != true)
            {
                return;
            }

            viewModel.ApplyExcludeMasks(dialogVm.IsEnabled, dialogVm.MasksText);
        }

        private void _CommitMaskIfInactive()
        {
            if (DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            if (MaskCombo.IsDropDownOpen || MaskCombo.IsKeyboardFocusWithin)
            {
                return;
            }

            viewModel.CommitMask();
        }

        private void _OnEntriesDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is FileListViewModel viewModel)
            {
                viewModel.OpenSelected();
            }
        }

        private void _OnEntriesKeyDown(object? sender, KeyEventArgs e)
        {
            if (_TryHandleThumbnailZoomKeys(e))
            {
                return;
            }

            if (e.Key != Key.Back)
            {
                return;
            }

            if (DataContext is FileListViewModel viewModel)
            {
                viewModel.GoUp();
            }

            e.Handled = true;
        }

        private void _OnThumbnailsPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers != KeyModifiers.Control)
            {
                return;
            }

            if (DataContext is not FileListViewModel viewModel || !viewModel.IsThumbnailsView)
            {
                return;
            }

            if (e.Delta.Y > 0)
            {
                viewModel.ZoomThumbnailsIn();
            }
            else if (e.Delta.Y < 0)
            {
                viewModel.ZoomThumbnailsOut();
            }

            e.Handled = true;
        }

        private bool _TryHandleThumbnailZoomKeys(KeyEventArgs e)
        {
            // Shift is allowed so Ctrl+Shift+= (the + key) zooms in on typical keyboards.
            var modifiersWithoutShift = e.KeyModifiers & ~KeyModifiers.Shift;
            if (modifiersWithoutShift != KeyModifiers.Control)
            {
                return false;
            }

            if (DataContext is not FileListViewModel viewModel || !viewModel.IsThumbnailsView)
            {
                return false;
            }

            if (viewModel.IsPathEditing)
            {
                return false;
            }

            if (e.Source is TextBox or ComboBox)
            {
                return false;
            }

            if (e.Key is Key.OemPlus or Key.Add)
            {
                viewModel.ZoomThumbnailsIn();
                e.Handled = true;
                return true;
            }

            if (e.Key is Key.OemMinus or Key.Subtract)
            {
                viewModel.ZoomThumbnailsOut();
                e.Handled = true;
                return true;
            }

            if (e.Key is Key.D0 or Key.NumPad0)
            {
                viewModel.ResetThumbnailSize();
                e.Handled = true;
                return true;
            }

            return false;
        }

        private void _OnReportGridLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid && DataContext is FileListViewModel viewModel)
            {
                _SyncReportSortGlyphs(grid, viewModel);
            }
        }

        private void _OnEntriesSorting(object? sender, DataGridColumnEventArgs e)
        {
            e.Handled = true;
            if (sender is not DataGrid grid || DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            viewModel.SortByColumn(e.Column.SortMemberPath);
            _SyncReportSortGlyphs(grid, viewModel);
        }

        private static void _SyncReportSortGlyphs(DataGrid grid, FileListViewModel viewModel)
        {
            var view = grid.CollectionView;
            if (view?.SortDescriptions is null)
            {
                return;
            }

            var direction = viewModel.IsSortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(
                    DataGridSortDescription.FromPath(nameof(FileListEntry.ListingGroup), ListSortDirection.Ascending)
                );
                view.SortDescriptions.Add(
                    DataGridSortDescription.FromPath(nameof(FileListEntry.IsDirectory), ListSortDirection.Descending)
                );
                view.SortDescriptions.Add(_CreateColumnSort(viewModel.SortMemberPath, direction));
            }
        }

        private static DataGridSortDescription _CreateColumnSort(string memberPath, ListSortDirection direction)
        {
            var isStringColumn = memberPath is nameof(FileListEntry.Name) or nameof(FileListEntry.Type);
            if (isStringColumn)
            {
                return DataGridSortDescription.FromPath(memberPath, direction, PathComparers.Os);
            }

            return DataGridSortDescription.FromPath(memberPath, direction);
        }
    }
}
