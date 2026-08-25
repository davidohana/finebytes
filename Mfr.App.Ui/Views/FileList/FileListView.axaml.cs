using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// File List pane host.
    /// </summary>
    public partial class FileListView : UserControl
    {
        private const double DragThreshold = 4;

        /// <summary>
        /// Rename List Add Selected command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddSelectedCommandProperty = AvaloniaProperty.Register<
            FileListView,
            ICommand?
        >(nameof(AddSelectedCommand));

        /// <summary>
        /// Rename List Add All command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddAllCommandProperty = AvaloniaProperty.Register<
            FileListView,
            ICommand?
        >(nameof(AddAllCommand));

        private FileListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;
        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private FileListEntry? _dragHitEntry;
        private object? _dragHitSource;
        private IReadOnlyList<FileListEntry>? _dragSelectionSnapshot;
        private bool _isDragPending;

        /// <summary>
        /// Gets or sets the Rename List Add Selected command.
        /// </summary>
        public ICommand? AddSelectedCommand
        {
            get => GetValue(AddSelectedCommandProperty);
            set => SetValue(AddSelectedCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the Rename List Add All command.
        /// </summary>
        public ICommand? AddAllCommand
        {
            get => GetValue(AddAllCommandProperty);
            set => SetValue(AddAllCommandProperty, value);
        }

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
            ReportGrid.CellPointerPressed += _OnReportCellPointerPressed;
            _WireListingDrag(ReportGrid);
            _WireListBoxDrag(ListViewList);
            _WireListBoxDrag(SmallIconsList);
            _WireListBoxDrag(LargeIconsList);
            _WireListBoxDrag(TilesList);
            _WireListBoxDrag(ThumbnailsList);
        }

        private void _WireListBoxDrag(ListBox listBox)
        {
            listBox.AddHandler(PointerPressedEvent, _OnListBoxPointerPressed, RoutingStrategies.Tunnel);
            _WireListingDrag(listBox);
        }

        private void _WireListingDrag(Control host)
        {
            host.AddHandler(PointerMovedEvent, _OnListingPointerMoved, RoutingStrategies.Tunnel);
            host.AddHandler(PointerReleasedEvent, _OnListingPointerReleased, RoutingStrategies.Tunnel);
            host.AddHandler(PointerCaptureLostEvent, _OnListingPointerCaptureLost, RoutingStrategies.Tunnel);
        }

        private void _OnReportCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            if (!_IsActiveListingSender(ReportGrid))
            {
                return;
            }

            _BeginPotentialDrag(e.Row?.DataContext as FileListEntry, e.PointerPressedEventArgs);
        }

        private void _OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_IsActiveListingSender(sender))
            {
                return;
            }

            _BeginPotentialDrag(_FindEntryFromSource(e.Source), e);
        }

        private void _BeginPotentialDrag(FileListEntry? hit, PointerEventArgs e)
        {
            _ClearDragState();

            if (_viewModel is null || hit is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            // Tunnel PointerPressed runs before the listing updates selection. Snapshot a
            // multi-selection when pressing an already-selected row so a later drag can
            // restore it (Avalonia collapses to the pressed row on the bubble pass).
            var isExtendingSelection =
                e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var alreadySelected = _viewModel.SelectedEntries.Any(entry =>
                PathComparers.Os.Equals(entry.FullPath, hit.FullPath)
            );
            if (alreadySelected && !isExtendingSelection && _viewModel.SelectedEntries.Count > 1)
            {
                _dragSelectionSnapshot = [.. _viewModel.SelectedEntries];
            }

            _dragHitEntry = hit;
            _dragHitSource = e.Source;
            _dragStartPoint = e.GetPosition(this);
            _dragStartArgs = e;
            _isDragPending = true;
        }

        private void _EnsureEntrySelectedForDrag(FileListEntry hit, object? source, KeyModifiers modifiers)
        {
            if (_viewModel is null)
            {
                return;
            }

            // Let the listing control own toggle/range selection while modifiers are held.
            var isExtendingSelection = modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Shift);
            if (isExtendingSelection)
            {
                return;
            }

            var alreadySelected = _viewModel.SelectedEntries.Any(entry =>
                PathComparers.Os.Equals(entry.FullPath, hit.FullPath)
            );
            if (alreadySelected)
            {
                return;
            }

            _ApplySelectionForDrag([hit], hit, source);
        }

        private void _RestoreDragSelectionSnapshot()
        {
            if (_viewModel is null || _dragSelectionSnapshot is not { Count: > 0 } snapshot)
            {
                return;
            }

            var pathToEntry = _viewModel.Entries.ToDictionary(entry => entry.FullPath, PathComparers.Os);
            var restored = new List<FileListEntry>(snapshot.Count);
            foreach (var entry in snapshot)
            {
                if (pathToEntry.TryGetValue(entry.FullPath, out var current))
                {
                    restored.Add(current);
                    continue;
                }

                restored.Add(entry);
            }

            if (restored.Count == 0)
            {
                return;
            }

            FileListEntry? focused = null;
            if (_dragHitEntry is not null)
            {
                focused = restored.FirstOrDefault(entry =>
                    PathComparers.Os.Equals(entry.FullPath, _dragHitEntry.FullPath)
                );
            }

            _ApplySelectionForDrag(restored, focused ?? restored[^1], _dragHitSource);
        }

        private void _ApplySelectionForDrag(IReadOnlyList<FileListEntry> entries, FileListEntry focused, object? source)
        {
            if (_viewModel is null)
            {
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                _viewModel.SetSelectedEntries(entries, focused);
            }
            finally
            {
                _selectionChangeFromView = false;
            }

            var host = _FindListingHost(source) ?? _GetActiveListingHost();
            if (host is null)
            {
                return;
            }

            _isSyncingSelection = true;
            try
            {
                _ApplySelectionToSender(host, force: true);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private async void _OnListingPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragPending || _dragStartPoint is null || _dragStartArgs is null || _viewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            var delta = e.GetPosition(this) - _dragStartPoint.Value;
            var movedFarEnough = Math.Abs(delta.X) >= DragThreshold || Math.Abs(delta.Y) >= DragThreshold;
            if (!movedFarEnough)
            {
                return;
            }

            if (_dragSelectionSnapshot is { Count: > 0 })
            {
                _RestoreDragSelectionSnapshot();
            }
            else if (_dragHitEntry is not null)
            {
                _EnsureEntrySelectedForDrag(_dragHitEntry, _dragHitSource, _dragStartArgs.KeyModifiers);
            }

            var paths = _GetAddableSelectedPaths();
            var dragArgs = _dragStartArgs;
            _ClearDragState();
            if (paths.Count == 0)
            {
                return;
            }

            var dataTransfer = await _BuildFileDataTransferAsync(paths).ConfigureAwait(true);
            if (dataTransfer is null)
            {
                return;
            }

            await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Copy).ConfigureAwait(true);
        }

        private async Task<DataTransfer?> _BuildFileDataTransferAsync(IReadOnlyList<string> paths)
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null)
            {
                return null;
            }

            var dataTransfer = new DataTransfer();
            foreach (var path in paths)
            {
                var item = await _TryGetStorageItemAsync(storage, path).ConfigureAwait(true);
                if (item is null)
                {
                    continue;
                }

                dataTransfer.Add(DataTransferItem.CreateFile(item));
            }

            if (dataTransfer.Items.Count == 0)
            {
                return null;
            }

            return dataTransfer;
        }

        private static async Task<IStorageItem?> _TryGetStorageItemAsync(IStorageProvider storage, string path)
        {
            if (Directory.Exists(path))
            {
                return await storage.TryGetFolderFromPathAsync(path).ConfigureAwait(true);
            }

            return await storage.TryGetFileFromPathAsync(path).ConfigureAwait(true);
        }

        private void _OnListingPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Explorer: press on a multi-selected row without dragging collapses to that row on release.
            if (
                _isDragPending
                && _dragSelectionSnapshot is { Count: > 0 }
                && _dragHitEntry is not null
                && _viewModel is not null
            )
            {
                var hitPath = _dragHitEntry.FullPath;
                var hit =
                    _viewModel.Entries.FirstOrDefault(entry => PathComparers.Os.Equals(entry.FullPath, hitPath))
                    ?? _dragHitEntry;
                _ApplySelectionForDrag([hit], hit, _dragHitSource ?? sender);
            }

            _ClearDragState();
        }

        private void _OnListingPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
        }

        private void _ClearDragState()
        {
            _isDragPending = false;
            _dragStartPoint = null;
            _dragStartArgs = null;
            _dragHitEntry = null;
            _dragHitSource = null;
            _dragSelectionSnapshot = null;
        }

        private IReadOnlyList<string> _GetAddableSelectedPaths()
        {
            if (_viewModel is null)
            {
                return [];
            }

            return
            [
                .. _viewModel
                    .SelectedEntries.Where(entry => RenameListAddSourceResolver.IsValidSourcePath(entry.FullPath))
                    .Select(entry => entry.FullPath),
            ];
        }

        private Control? _FindListingHost(object? source)
        {
            for (var current = source as Visual; current is not null; current = current.GetVisualParent())
            {
                if (
                    ReferenceEquals(current, ReportGrid)
                    || ReferenceEquals(current, ListViewList)
                    || ReferenceEquals(current, SmallIconsList)
                    || ReferenceEquals(current, LargeIconsList)
                    || ReferenceEquals(current, TilesList)
                    || ReferenceEquals(current, ThumbnailsList)
                )
                {
                    return (Control)current;
                }
            }

            return null;
        }

        private Control? _GetActiveListingHost()
        {
            if (_viewModel is null)
            {
                return null;
            }

            if (_viewModel.IsReportView)
            {
                return ReportGrid;
            }

            if (_viewModel.IsListView)
            {
                return ListViewList;
            }

            if (_viewModel.IsSmallIconsView)
            {
                return SmallIconsList;
            }

            if (_viewModel.IsLargeIconsView)
            {
                return LargeIconsList;
            }

            if (_viewModel.IsTilesView)
            {
                return TilesList;
            }

            if (_viewModel.IsThumbnailsView)
            {
                return ThumbnailsList;
            }

            return null;
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
                _CommitMask(viewModel);
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

            _CommitMask(viewModel);
        }

        private void _CommitMask(FileListViewModel viewModel)
        {
            var mask = viewModel.Mask;
            viewModel.CommitMask();
            _SyncMaskComboText(mask);
            Dispatcher.UIThread.Post(() => _SyncMaskComboText(mask), DispatcherPriority.Background);
        }

        private void _SyncMaskComboText(string mask)
        {
            if (!string.Equals(MaskCombo.Text, mask, StringComparison.Ordinal))
            {
                MaskCombo.Text = mask;
            }
        }

        private void _OnEntriesDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is FileListViewModel viewModel)
            {
                viewModel.OpenSelected();
            }
        }

        private void _OnEntriesContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (_viewModel is null || !_IsActiveListingSender(sender))
            {
                return;
            }

            var hit = _FindEntryFromSource(e.Source);
            if (hit is null)
            {
                return;
            }

            var alreadySelected = _viewModel.SelectedEntries.Any(entry =>
                PathComparers.Os.Equals(entry.FullPath, hit.FullPath)
            );
            if (alreadySelected)
            {
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                _viewModel.SetSelectedEntries([hit], hit);
            }
            finally
            {
                _selectionChangeFromView = false;
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
        }

        private static FileListEntry? _FindEntryFromSource(object? source)
        {
            for (var current = source as Visual; current is not null; current = current.GetVisualParent())
            {
                if (current is ListBoxItem { DataContext: FileListEntry listEntry })
                {
                    return listEntry;
                }

                if (current is DataGridRow { DataContext: FileListEntry gridEntry })
                {
                    return gridEntry;
                }
            }

            return null;
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

            // Keep multi-select visible while a drag may start: Avalonia collapses on press, so
            // undo that synchronously here instead of waiting for the drag threshold (avoids flicker).
            if (_isDragPending && _dragSelectionSnapshot is { Count: > 0 } snapshot)
            {
                var hostItems = sender switch
                {
                    ListBox listBox => listBox.SelectedItems,
                    DataGrid grid => grid.SelectedItems,
                    _ => null,
                };
                if (hostItems is IList selectedItems && !_SelectionMatchesByPath(selectedItems, snapshot))
                {
                    _RestoreDragSelectionSnapshot();
                }

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
