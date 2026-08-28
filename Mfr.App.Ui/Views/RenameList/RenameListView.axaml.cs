using System.Collections;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Input;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Rename List pane host.
    /// </summary>
    public partial class RenameListView : UserControl
    {
        private const string DropMarkClass = "drop-mark";
        private const double DragThreshold = 4;

        /// <summary>
        /// Application format for dragging Rename List rows to reorder within the grid.
        /// </summary>
        internal static readonly DataFormat<string> InternalReorderFormat = DataFormat.CreateStringApplicationFormat(
            "mfr-rename-list-reorder"
        );

        private RenameListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;
        private DataGridColumn? _lastHintColumn;
        private AddProgressDialog? _addProgressDialog;
        private RenameListFieldShuttleDialog? _fieldShuttleDialog;
        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private RenameListEntry? _dragHitEntry;
        private IReadOnlyList<RenameListEntry>? _dragSelectionSnapshot;
        private KeyModifiers _lastSortClickModifiers = KeyModifiers.None;

        /// <summary>
        /// Initializes the Rename List pane.
        /// </summary>
        public RenameListView()
        {
            InitializeComponent();
            DataContextChanged += _OnDataContextChanged;
            RenameGrid.SelectionChanged += _OnSelectionChanged;
            RenameGrid.CurrentCellChanged += _OnCurrentCellChanged;
            RenameGrid.CellPointerPressed += _OnCellPointerPressed;
            RenameGrid.LoadingRow += _OnLoadingRow;
            RenameGrid.AddHandler(KeyDownEvent, _OnGridKeyDown, RoutingStrategies.Tunnel);
            _WireHeaderContextMenu();
            _WireColumnReorder();
            RenameGrid.AddHandler(PointerMovedEvent, _OnGridPointerMoved, RoutingStrategies.Tunnel);
            RenameGrid.AddHandler(PointerReleasedEvent, _OnGridPointerReleased, RoutingStrategies.Tunnel);
            RenameGrid.AddHandler(PointerCaptureLostEvent, _OnGridPointerCaptureLost, RoutingStrategies.Tunnel);
            RenameGrid.AddHandler(PointerPressedEvent, _OnGridPointerPressed, RoutingStrategies.Tunnel);
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, _OnDragOver);
            AddHandler(DragDrop.DragLeaveEvent, _OnDragLeave);
            AddHandler(DragDrop.DropEvent, _OnDrop);
        }

        private void _OnFieldShuttleRequested(object? sender, RenameListFieldShuttleTab tab)
        {
            // Defer until any context menu closes so the modal centers on the owner window.
            Dispatcher.UIThread.Post(() => _ = _ShowFieldShuttleDialogAsync(tab));
        }

        private async Task _ShowFieldShuttleDialogAsync(RenameListFieldShuttleTab tab)
        {
            if (_viewModel is null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            if (_fieldShuttleDialog is not null)
            {
                return;
            }

            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                _viewModel.VisibleColumns,
                _viewModel.SortKeys,
                tab
            );
            var dialog = new RenameListFieldShuttleDialog(dialogVm);
            _fieldShuttleDialog = dialog;
            try
            {
                var accepted = await dialog.ShowDialog<bool?>(owner);
                if (accepted != true)
                {
                    return;
                }

                _viewModel.SetVisibleColumns(dialogVm.ResultColumns);
                _viewModel.SetSortKeys(dialogVm.ResultSortKeys);
            }
            finally
            {
                if (ReferenceEquals(_fieldShuttleDialog, dialog))
                {
                    _fieldShuttleDialog = null;
                }
            }
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel is not null)
            {
                _viewModel.FieldShuttleRequested -= _OnFieldShuttleRequested;
                _viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
                _viewModel.AddProgress.PropertyChanged -= _OnAddProgressPropertyChanged;
            }

            _viewModel = DataContext as RenameListViewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.FieldShuttleRequested += _OnFieldShuttleRequested;
            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _viewModel.AddProgress.PropertyChanged += _OnAddProgressPropertyChanged;
            _RebuildColumns();
            _SyncSelectionToGrid();
            _ApplyDropMarkVisuals();
            _ClearSortDescriptions();
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_TryHandleRenameListShortcut(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void _OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            // Tunnel so Ctrl+Up/Down are not consumed by DataGrid navigation.
            _ = _TryHandleRenameListShortcut(e);
        }

        private bool _TryHandleRenameListShortcut(KeyEventArgs e)
        {
            if (_viewModel is null || _viewModel.IsAdding || e.Handled)
            {
                return false;
            }

            if (_MatchesGesture(e, AppShortcuts.RemoveSelectedDelete))
            {
                if (_viewModel.RemoveSelectedCommand.CanExecute(null))
                {
                    _viewModel.RemoveSelectedCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.LocateInFileList))
            {
                if (_viewModel.LocateInFileListCommand.CanExecute(null))
                {
                    _viewModel.LocateInFileListCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.MoveSelectedUp))
            {
                if (_viewModel.MoveSelectedUpCommand.CanExecute(null))
                {
                    _viewModel.MoveSelectedUpCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.MoveSelectedDown))
            {
                if (_viewModel.MoveSelectedDownCommand.CanExecute(null))
                {
                    _viewModel.MoveSelectedDownCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            return false;
        }

        private static bool _MatchesGesture(KeyEventArgs e, KeyGesture gesture)
        {
            return e.Key == gesture.Key && e.KeyModifiers == gesture.KeyModifiers;
        }

        private void _OnCurrentCellChanged(object? sender, EventArgs e)
        {
            _PublishFocusedCellHint();
        }

        private void _OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            _PublishCellHint(e.Row?.DataContext as RenameListEntry, e.Column);
            _BeginPotentialDrag(e.Row?.DataContext as RenameListEntry, e.PointerPressedEventArgs);
        }

        private void _BeginPotentialDrag(RenameListEntry? hit, PointerEventArgs e)
        {
            _ClearDragState();

            if (_viewModel is null || hit is null || _viewModel.IsAdding)
            {
                return;
            }

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            // Avalonia collapses multi-selection to the pressed row. Snapshot when pressing an
            // already-selected row so SelectionChanged can undo that immediately (File List pattern).
            var isExtendingSelection =
                e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var alreadySelected = _viewModel.SelectedEntries.Contains(hit);
            if (alreadySelected && !isExtendingSelection && _viewModel.SelectedEntries.Count > 1)
            {
                _dragSelectionSnapshot = [.. _viewModel.SelectedEntries];
            }

            _dragHitEntry = hit;
            _dragStartPoint = e.GetPosition(this);
            _dragStartArgs = e;
        }

        private async void _OnGridPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartArgs is null || _dragStartPoint is null || _viewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            var delta = e.GetPosition(this) - _dragStartPoint.Value;
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            {
                return;
            }

            if (_dragSelectionSnapshot is { Count: > 0 })
            {
                _ApplySelectionForDrag(_dragSelectionSnapshot);
            }

            var dragArgs = _dragStartArgs;
            _ClearDragState();
            if (_viewModel.SelectedEntries.Count == 0)
            {
                return;
            }

            _viewModel.CancelAutoSort();

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(InternalReorderFormat, "1"));
            try
            {
                await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Move).ConfigureAwait(true);
            }
            finally
            {
                _ClearDropMark();
            }
        }

        private void _OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_dragSelectionSnapshot is { Count: > 0 } && _dragHitEntry is not null)
            {
                _ApplySelectionForDrag([_dragHitEntry]);
            }

            _ClearDragState();
        }

        private void _OnGridPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
        }

        private void _ApplySelectionForDrag(IReadOnlyList<RenameListEntry> entries)
        {
            if (_viewModel is null)
            {
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                _viewModel.SetSelectedEntries(entries);
            }
            finally
            {
                _selectionChangeFromView = false;
            }

            _SyncSelectionToGrid();
        }

        private void _ClearDragState()
        {
            _dragStartPoint = null;
            _dragStartArgs = null;
            _dragHitEntry = null;
            _dragSelectionSnapshot = null;
        }

        private void _PublishFocusedCellHint()
        {
            if (_viewModel is null)
            {
                return;
            }

            _PublishCellHint(_ReadFocusedEntry(), RenameGrid.CurrentColumn ?? _lastHintColumn);
        }

        private RenameListEntry? _ReadFocusedEntry()
        {
            var selected = _viewModel?.SelectedEntries;
            if (selected is { Count: > 0 })
            {
                return selected[^1];
            }

            if (RenameGrid.SelectedItem is RenameListEntry entry)
            {
                return entry;
            }

            return null;
        }

        private void _PublishCellHint(RenameListEntry? entry, DataGridColumn? column)
        {
            if (_viewModel is null)
            {
                return;
            }

            if (entry is null || column is null)
            {
                _viewModel.CellStatusHintDisplay = StatusHintDisplay.Empty;
                return;
            }

            var fieldKey = RenameListGridColumns.GetFieldKey(column);
            if (fieldKey is null)
            {
                _viewModel.CellStatusHintDisplay = StatusHintDisplay.Empty;
                return;
            }

            if (!RenameListFieldCatalog.TryGetField(fieldKey.Value, out var field))
            {
                _viewModel.CellStatusHintDisplay = StatusHintDisplay.Empty;
                return;
            }

            _lastHintColumn = column;

            var columnHeader = RenameListFieldDisplay.GetColumnHeaderText(field, fieldKey.Value.IsPreview);
            var cellText = entry.GetFieldText(fieldKey.Value);
            _viewModel.CellStatusHintDisplay = RenameListCellHint.FormatParts(
                columnHeader,
                cellText,
                fieldKey.Value.IsPreview
            );
        }

        private void _OnDragOver(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            var isReorder = _IsInternalReorder(e);
            var canAccept = isReorder || _CanAcceptFileDrop(e);
            if (_viewModel is null || _viewModel.IsAdding || !canAccept)
            {
                e.DragEffects = DragDropEffects.None;
                _ClearDropMark();
                return;
            }

            // Match MFR7: Alt while dragging external files over Rename List clears it immediately.
            // File payloads only (File List / Explorer); internal reorder does not clear.
            if (!isReorder && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                _viewModel.Clear();
                _ClearDropMark();
                e.DragEffects = DragDropEffects.Copy;
                return;
            }

            if (isReorder)
            {
                _viewModel.CancelAutoSort();
            }

            _UpdateDropMarkFromPointer(e);
            e.DragEffects = isReorder ? DragDropEffects.Move : DragDropEffects.Copy;
        }

        private void _OnDragLeave(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            // Avalonia raises DragLeave when crossing child visuals; only clear when the pointer
            // actually left this control.
            var position = e.GetPosition(this);
            if (new Rect(Bounds.Size).Contains(position))
            {
                return;
            }

            _ClearDropMark();
        }

        private async void _OnDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            if (_viewModel is null || _viewModel.IsAdding)
            {
                _ClearDropMark();
                return;
            }

            if (_IsInternalReorder(e))
            {
                _ = _viewModel.ReorderSelectedToDropMark();
                return;
            }

            if (!_CanAcceptFileDrop(e))
            {
                _ClearDropMark();
                return;
            }

            var paths = _ReadDroppedFilePaths(e);
            if (paths.Count == 0)
            {
                _ClearDropMark();
                return;
            }

            // DropMarkIndex is consumed (then cleared) inside AddPathsAsync / _AddSourcesAsync.
            await _viewModel.AddPathsAsync(paths).ConfigureAwait(true);
            _ApplyDropMarkVisuals();
        }

        private void _UpdateDropMarkFromPointer(DragEventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            var row = _HitTestDataGridRow(e);
            if (row is null)
            {
                // Keep the last mark when the pointer is briefly between cells / over gaps so the
                // salmon highlight does not flicker during DragOver.
                return;
            }

            var index = row.Index;
            if (index < 0 || index >= _viewModel.Entries.Count)
            {
                return;
            }

            _viewModel.SetDropMarkIndex(index);
        }

        private DataGridRow? _HitTestDataGridRow(DragEventArgs e)
        {
            var position = e.GetPosition(RenameGrid);
            foreach (var row in RenameGrid.GetVisualDescendants().OfType<DataGridRow>())
            {
                var origin = row.TranslatePoint(default, RenameGrid);
                if (origin is null)
                {
                    continue;
                }

                if (new Rect(origin.Value, row.Bounds.Size).Contains(position))
                {
                    return row;
                }
            }

            return null;
        }

        private void _ClearDropMark()
        {
            _viewModel?.SetDropMarkIndex(null);
        }

        private void _OnLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            _ApplyDropMarkClass(e.Row);
        }

        private void _ApplyDropMarkVisuals()
        {
            foreach (var row in RenameGrid.GetVisualDescendants().OfType<DataGridRow>())
            {
                _ApplyDropMarkClass(row);
            }
        }

        private void _ApplyDropMarkClass(DataGridRow row)
        {
            var isMarked = _viewModel?.DropMarkIndex is { } markIndex && row.Index == markIndex;
            row.Classes.Set(DropMarkClass, isMarked);
        }

        private static bool _CanAcceptFileDrop(DragEventArgs e)
        {
            return e.DataTransfer?.Formats.Contains(DataFormat.File) == true;
        }

        private static bool _IsInternalReorder(DragEventArgs e)
        {
            return e.DataTransfer?.Formats.Contains(InternalReorderFormat) == true;
        }

        private static List<string> _ReadDroppedFilePaths(DragEventArgs e)
        {
            var files = e.DataTransfer?.TryGetFiles();
            if (files is null || files.Length == 0)
            {
                return [];
            }

            var paths = new List<string>(files.Length);
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath();
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                paths.Add(path);
            }

            return paths;
        }

        private void _OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            // Undo Avalonia's press collapse before paint so a multi-select drag has no flicker.
            if (_dragSelectionSnapshot is { Count: > 0 } snapshot)
            {
                _ApplySelectionForDrag(snapshot);
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                var selected = _ReadSelectedEntries();
                if (_ContainsStaleSelection(selected))
                {
                    return;
                }

                _viewModel.SetSelectedEntries(selected);
                _PublishFocusedCellHint();
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

            if (e.PropertyName is nameof(RenameListViewModel.SelectedEntries))
            {
                _SyncSelectionToGrid();
                _PublishFocusedCellHint();
            }

            if (e.PropertyName is nameof(RenameListViewModel.DropMarkIndex))
            {
                _ApplyDropMarkVisuals();
            }

            if (e.PropertyName is nameof(RenameListViewModel.VisibleColumns))
            {
                _RebuildColumns();
            }
        }

        private void _OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is not Visual source || source.FindAncestorOfType<DataGridColumnHeader>() is null)
            {
                return;
            }

            _lastSortClickModifiers = e.KeyModifiers;
        }

        private void _OnEntriesSorting(object? sender, DataGridColumnEventArgs e)
        {
            e.Handled = true;
            if (_viewModel is null)
            {
                return;
            }

            var append = _lastSortClickModifiers.HasFlag(KeyModifiers.Shift);
            _lastSortClickModifiers = KeyModifiers.None;

            var fieldKey = RenameListGridColumns.GetFieldKey(e.Column);
            if (fieldKey is not null)
            {
                _viewModel.SortByFieldKey(fieldKey.Value, append);
            }

            _ClearSortDescriptions();
        }

        private void _ClearSortDescriptions()
        {
            var view = RenameGrid.CollectionView;
            if (view?.SortDescriptions is null)
            {
                return;
            }

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
            }
        }

        private void _OnAddProgressPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RenameListAddProgressViewModel.IsDialogVisible))
            {
                _ = _SyncAddProgressDialogAsync();
            }
        }

        private async Task _SyncAddProgressDialogAsync()
        {
            if (_viewModel is null)
            {
                return;
            }

            if (!_viewModel.AddProgress.IsDialogVisible)
            {
                _addProgressDialog?.Close();
                return;
            }

            if (_addProgressDialog is not null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new AddProgressDialog(_viewModel.AddProgress);
            _addProgressDialog = dialog;
            try
            {
                await dialog.ShowDialog(owner);
            }
            finally
            {
                if (ReferenceEquals(_addProgressDialog, dialog))
                {
                    _addProgressDialog = null;
                }
            }
        }

        private void _SyncSelectionToGrid()
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            if (RenameGrid.SelectedItems is not IList selectedItems)
            {
                return;
            }

            if (_SelectionMatches(selectedItems, _viewModel.SelectedEntries))
            {
                return;
            }

            _isSyncingSelection = true;
            try
            {
                selectedItems.Clear();
                foreach (var entry in _viewModel.SelectedEntries)
                {
                    selectedItems.Add(entry);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private bool _ContainsStaleSelection(IReadOnlyList<RenameListEntry> selected)
        {
            if (_viewModel is null || selected.Count == 0)
            {
                return false;
            }

            var entries = _viewModel.Entries;
            foreach (var entry in selected)
            {
                if (!entries.Contains(entry))
                {
                    return true;
                }
            }

            return false;
        }

        private IReadOnlyList<RenameListEntry> _ReadSelectedEntries()
        {
            if (RenameGrid.SelectedItems is not IList items)
            {
                return [];
            }

            return [.. items.OfType<RenameListEntry>()];
        }

        private static bool _SelectionMatches(IList selectedItems, IReadOnlyList<RenameListEntry> expected)
        {
            if (selectedItems.Count != expected.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!ReferenceEquals(selectedItems[i], expected[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
