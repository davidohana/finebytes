using System.Collections;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// File List pane host.
    /// </summary>
    public partial class FileListView : UserControl
    {
        private FileListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;

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
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            _viewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _viewModel = DataContext as FileListViewModel;
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
                _SyncSelectionToActiveListing();
            }
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

        private void _OnEntriesSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            if (!_IsActiveListingSender(sender))
            {
                return;
            }

            var selected = _ReadSelectedEntries(sender);
            var focused = sender switch
            {
                ListBox listBox => listBox.SelectedItem as FileListEntry,
                DataGrid grid => grid.SelectedItem as FileListEntry,
                _ => null,
            };

            _selectionChangeFromView = true;
            try
            {
                _viewModel.SetSelectedEntries(selected, focused);
            }
            finally
            {
                _selectionChangeFromView = false;
            }
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_selectionChangeFromView)
            {
                return;
            }

            if (
                e.PropertyName
                is nameof(FileListViewModel.SelectedEntries)
                    or nameof(FileListViewModel.ViewMode)
                    or nameof(FileListViewModel.SelectedEntry)
            )
            {
                _SyncSelectionToActiveListing();
            }
        }

        private void _SyncSelectionToActiveListing()
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            _isSyncingSelection = true;
            try
            {
                if (_viewModel.IsReportView)
                {
                    _ApplySelection(ReportGrid);
                    return;
                }

                if (_viewModel.IsListView)
                {
                    _ApplySelection(ListViewList);
                    return;
                }

                if (_viewModel.IsSmallIconsView)
                {
                    _ApplySelection(SmallIconsList);
                    return;
                }

                if (_viewModel.IsLargeIconsView)
                {
                    _ApplySelection(LargeIconsList);
                    return;
                }

                if (_viewModel.IsTilesView)
                {
                    _ApplySelection(TilesList);
                    return;
                }

                if (_viewModel.IsThumbnailsView)
                {
                    _ApplySelection(ThumbnailsList);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void _ApplySelection(ListBox listBox, bool force = false)
        {
            if (_viewModel is null)
            {
                return;
            }

            if (listBox.SelectedItems is not IList selectedItems)
            {
                return;
            }

            if (!force && _SelectionMatchesByPath(selectedItems, _viewModel.SelectedEntries))
            {
                return;
            }

            selectedItems.Clear();
            FileListEntry? focusedEntry = null;
            foreach (var entry in _viewModel.SelectedEntries)
            {
                if (ReferenceEquals(entry, _viewModel.SelectedEntry))
                {
                    focusedEntry = entry;
                    continue;
                }

                selectedItems.Add(entry);
            }

            if (focusedEntry is not null)
            {
                selectedItems.Add(focusedEntry);
            }
        }

        private void _ApplySelection(DataGrid grid, bool force = false)
        {
            if (_viewModel is null)
            {
                return;
            }

            if (grid.SelectedItems is not IList selectedItems)
            {
                return;
            }

            if (!force && _SelectionMatchesByPath(selectedItems, _viewModel.SelectedEntries))
            {
                return;
            }

            selectedItems.Clear();
            foreach (var entry in _viewModel.SelectedEntries)
            {
                selectedItems.Add(entry);
            }

            if (_viewModel.SelectedEntries.Count == 1)
            {
                grid.SelectedItem = _viewModel.SelectedEntry;
            }
        }

        private void _ApplySelectionToSender(object? sender, bool force = false)
        {
            switch (sender)
            {
                case DataGrid grid:
                    _ApplySelection(grid, force);
                    break;
                case ListBox listBox:
                    _ApplySelection(listBox, force);
                    break;
                default:
                    break;
            }
        }

        private void _ScrollSelectedIntoView(object? sender)
        {
            if (_viewModel?.SelectedEntry is not { } focused)
            {
                return;
            }

            switch (sender)
            {
                case DataGrid grid:
                    grid.ScrollIntoView(focused, null);
                    break;
                case ListBox listBox:
                    listBox.ContainerFromItem(focused)?.BringIntoView();
                    break;
                default:
                    break;
            }
        }

        private static IReadOnlyList<FileListEntry> _ReadSelectedEntries(object? sender)
        {
            return sender switch
            {
                ListBox listBox when listBox.SelectedItems is IList items => [.. items.Cast<FileListEntry>()],
                DataGrid grid when grid.SelectedItems is IList items => [.. items.Cast<FileListEntry>()],
                _ => [],
            };
        }

        private static bool _SelectionMatchesByPath(IList selectedItems, IReadOnlyList<FileListEntry> expected)
        {
            if (selectedItems.Count != expected.Count)
            {
                return false;
            }

            if (expected.Count == 0)
            {
                return selectedItems.Count == 0;
            }

            var expectedPaths = expected.Select(entry => entry.FullPath).ToHashSet(PathComparers.Os);
            foreach (var item in selectedItems)
            {
                if (item is not FileListEntry entry || !expectedPaths.Contains(entry.FullPath))
                {
                    return false;
                }
            }

            return true;
        }

        private bool _IsActiveListingSender(object? sender)
        {
            if (_viewModel is null)
            {
                return false;
            }

            if (_viewModel.IsReportView)
            {
                return ReferenceEquals(sender, ReportGrid);
            }

            if (_viewModel.IsListView)
            {
                return ReferenceEquals(sender, ListViewList);
            }

            if (_viewModel.IsSmallIconsView)
            {
                return ReferenceEquals(sender, SmallIconsList);
            }

            if (_viewModel.IsLargeIconsView)
            {
                return ReferenceEquals(sender, LargeIconsList);
            }

            if (_viewModel.IsTilesView)
            {
                return ReferenceEquals(sender, TilesList);
            }

            if (_viewModel.IsThumbnailsView)
            {
                return ReferenceEquals(sender, ThumbnailsList);
            }

            return false;
        }

        private void _OnEntriesKeyDown(object? sender, KeyEventArgs e)
        {
            if (_TryHandleThumbnailZoomKeys(e))
            {
                return;
            }

            if (_TryHandleArrowNavigation(sender, e))
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

        private bool _TryHandleArrowNavigation(object? sender, KeyEventArgs e)
        {
            if (_viewModel is null || e.Key is not (Key.Up or Key.Down))
            {
                return false;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                return false;
            }

            if (e.KeyModifiers is not KeyModifiers.None)
            {
                return false;
            }

            if (!_IsActiveListingSender(sender))
            {
                return false;
            }

            var delta = e.Key == Key.Down ? 1 : -1;
            if (!_viewModel.TryMoveSelection(delta))
            {
                e.Handled = true;
                return true;
            }

            _isSyncingSelection = true;
            try
            {
                _ApplySelectionToSender(sender, force: true);
            }
            finally
            {
                _isSyncingSelection = false;
            }

            _ScrollSelectedIntoView(sender);
            e.Handled = true;
            return true;
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
