using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Input;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Rename List pane host.
    /// </summary>
    public partial class RenameListView : UserControl
    {
        private const double PointerHintMoveThreshold = 8.0;

        private RenameListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;
        private bool _isPointerOverGrid;
        private bool _freezeHint;
        private bool _suppressCellPressUnfreeze;
        private RenameListEntry? _frozenHintEntry;
        private DataGridColumn? _frozenHintColumn;
        private Point? _pointerHintAnchor;
        private DataGridColumn? _lastHintColumn;
        private AddProgressDialog? _addProgressDialog;
        private double? _savedVerticalScroll;

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
            RenameGrid.PointerEntered += _OnGridPointerEntered;
            RenameGrid.PointerExited += _OnGridPointerExited;
            RenameGrid.AddHandler(PointerMovedEvent, _OnGridPointerMoved, RoutingStrategies.Tunnel);
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, _OnDragOver);
            AddHandler(DragDrop.DropEvent, _OnDrop);
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
                _viewModel.SelectedEntriesRemoving -= _OnSelectedEntriesRemoving;
                _viewModel.AddProgress.PropertyChanged -= _OnAddProgressPropertyChanged;
                _viewModel.Entries.CollectionChanged -= _OnEntriesCollectionChanged;
            }

            _viewModel = DataContext as RenameListViewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _viewModel.SelectedEntriesRemoving += _OnSelectedEntriesRemoving;
            _viewModel.AddProgress.PropertyChanged += _OnAddProgressPropertyChanged;
            _viewModel.Entries.CollectionChanged += _OnEntriesCollectionChanged;
            _SyncSelectionToGrid();
        }

        private void _OnSelectedEntriesRemoving(object? sender, EventArgs e)
        {
            _BeginHintFreeze();
        }

        private void _OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset)
            {
                _BeginHintFreeze();
            }
        }

        private void _BeginHintFreeze()
        {
            _freezeHint = true;
            // Context-menu / toolbar remove: cursor may already be far from the row. Re-anchor on
            // the next pointer event so a menu click does not immediately clear the freeze.
            _pointerHintAnchor = null;
            _suppressCellPressUnfreeze = true;
            _CaptureVerticalScroll();
            _SetHintFrozenClass(true);
            Dispatcher.UIThread.Post(_LiftCellPressUnfreezeSuppress, DispatcherPriority.Background);
        }

        private void _LiftCellPressUnfreezeSuppress()
        {
            _suppressCellPressUnfreeze = false;
        }

        private void _EndHintFreeze()
        {
            if (!_freezeHint && _frozenHintEntry is null)
            {
                return;
            }

            _freezeHint = false;
            _suppressCellPressUnfreeze = false;
            _frozenHintEntry = null;
            _frozenHintColumn = null;
            _pointerHintAnchor = null;
            _savedVerticalScroll = null;
            _SetHintFrozenClass(false);
        }

        private bool _IsHintFrozenAt(Point position)
        {
            if (!_freezeHint)
            {
                return false;
            }

            if (_pointerHintAnchor is not { } anchor)
            {
                return true;
            }

            var deltaX = position.X - anchor.X;
            var deltaY = position.Y - anchor.Y;
            var movedFarEnough =
                Math.Abs(deltaX) >= PointerHintMoveThreshold || Math.Abs(deltaY) >= PointerHintMoveThreshold;
            return !movedFarEnough;
        }

        private void _CaptureFrozenHint()
        {
            var selected = _viewModel?.SelectedEntries;
            _frozenHintEntry = selected is { Count: > 0 } ? selected[^1] : null;
            _frozenHintColumn = _lastHintColumn ?? RenameGrid.CurrentColumn;
        }

        private void _PublishFrozenHint()
        {
            _PublishCellHint(_frozenHintEntry, _frozenHintColumn);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel is null || _viewModel.IsAdding)
            {
                base.OnKeyDown(e);
                return;
            }

            if (_MatchesGesture(e, AppShortcuts.RemoveSelectedDelete))
            {
                if (_viewModel.RemoveSelectedCommand.CanExecute(null))
                {
                    _BeginHintFreeze();
                    _viewModel.RemoveSelectedCommand.Execute(null);
                    _CaptureFrozenHint();
                    _PublishFrozenHint();
                    _ScheduleVerticalScrollRestore();
                    e.Handled = true;
                    return;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.LocateInFileList))
            {
                if (_viewModel.LocateInFileListCommand.CanExecute(null))
                {
                    _viewModel.LocateInFileListCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        private static bool _MatchesGesture(KeyEventArgs e, KeyGesture gesture)
        {
            return e.Key == gesture.Key && e.KeyModifiers == gesture.KeyModifiers;
        }

        private void _OnCurrentCellChanged(object? sender, EventArgs e)
        {
            if (_freezeHint || !_isPointerOverGrid || _viewModel is null)
            {
                return;
            }

            _PublishCellHint(_ReadFocusedEntry(), RenameGrid.CurrentColumn);
        }

        private void _OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            var position = e.PointerPressedEventArgs.GetPosition(RenameGrid);

            // Menu-item click can deliver a press to the row under the menu after remove.
            if (_freezeHint && (_suppressCellPressUnfreeze || _pointerHintAnchor is null))
            {
                _pointerHintAnchor ??= position;
                return;
            }

            _EndHintFreeze();
            _PublishCellHint(e.Row?.DataContext as RenameListEntry, e.Column);
        }

        private void _OnGridPointerEntered(object? sender, PointerEventArgs e)
        {
            _isPointerOverGrid = true;
        }

        private void _OnGridPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPointerOverGrid || _viewModel is null)
            {
                return;
            }

            var position = e.GetPosition(RenameGrid);

            if (_freezeHint && _pointerHintAnchor is null)
            {
                _pointerHintAnchor = position;
                return;
            }

            if (_IsHintFrozenAt(position))
            {
                return;
            }

            _EndHintFreeze();
            var entry = _HitTestRowAt(position);
            _PublishCellHint(entry, RenameGrid.CurrentColumn ?? _lastHintColumn);
        }

        private void _OnGridPointerExited(object? sender, PointerEventArgs e)
        {
            // Virtualized row recycle can spuriously exit/re-enter while the cursor is unchanged.
            if (_freezeHint)
            {
                return;
            }

            _isPointerOverGrid = false;

            if (_viewModel is null)
            {
                return;
            }

            _viewModel.CellStatusHintDisplay = StatusHintDisplay.Empty;
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

            if (_freezeHint)
            {
                if (_frozenHintEntry is null)
                {
                    return;
                }

                entry = _frozenHintEntry;
                column = _frozenHintColumn ?? column;
            }

            if (entry is null || column?.Header is not string columnHeader || string.IsNullOrEmpty(columnHeader))
            {
                _viewModel.CellStatusHintDisplay = StatusHintDisplay.Empty;
                return;
            }

            _lastHintColumn = column;

            var cellText = RenameListCellHint.GetCellText(entry, columnHeader);
            _viewModel.CellStatusHintDisplay = RenameListCellHint.FormatParts(
                columnHeader,
                cellText,
                RenameListCellHint.IsPreviewColumn(columnHeader)
            );
        }

        private void _OnDragOver(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            // Match MFR7: Alt while dragging external files over Rename List clears it immediately.
            // Only file payloads (File List / Explorer); internal reorder would not clear.
            if (_viewModel is null || _viewModel.IsAdding || !_CanAcceptFileDrop(e))
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                _viewModel.Clear();
            }

            e.DragEffects = DragDropEffects.Copy;
        }

        private async void _OnDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            if (_viewModel is null || _viewModel.IsAdding || !_CanAcceptFileDrop(e))
            {
                return;
            }

            var paths = _ReadDroppedFilePaths(e);
            if (paths.Count == 0)
            {
                return;
            }

            await _viewModel.AddPathsAsync(paths).ConfigureAwait(true);
        }

        private static bool _CanAcceptFileDrop(DragEventArgs e)
        {
            return e.DataTransfer?.Formats.Contains(DataFormat.File) == true;
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
            if (_isSyncingSelection || _viewModel is null || _freezeHint)
            {
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
                if (!_isPointerOverGrid)
                {
                    _PublishFocusedCellHint();
                }
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
                if (_freezeHint)
                {
                    _CaptureFrozenHint();
                    _PublishFrozenHint();
                    _ScheduleVerticalScrollRestore();
                }
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

        private RenameListEntry? _HitTestRowAt(Point position)
        {
            var hit = RenameGrid.InputHitTest(position);
            return _HitTestRow(hit);
        }

        private static RenameListEntry? _HitTestRow(object? source)
        {
            if (source is DataGridRow row && row.DataContext is RenameListEntry rowEntry)
            {
                return rowEntry;
            }

            if (source is not Visual visual)
            {
                return null;
            }

            for (var current = visual; current is not null; current = current.GetVisualParent())
            {
                if (current is DataGridRow dataGridRow && dataGridRow.DataContext is RenameListEntry entry)
                {
                    return entry;
                }
            }

            return null;
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

        private void _SetHintFrozenClass(bool isFrozen)
        {
            RenameGrid.Classes.Set("hint-frozen", isFrozen);
        }

        private void _CaptureVerticalScroll()
        {
            _savedVerticalScroll ??= _ReadVerticalScroll();
        }

        private void _ScheduleVerticalScrollRestore()
        {
            if (_savedVerticalScroll is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(_RestoreVerticalScroll, DispatcherPriority.Loaded);
            Dispatcher.UIThread.Post(_RestoreVerticalScroll, DispatcherPriority.Render);
        }

        private void _RestoreVerticalScroll()
        {
            if (_savedVerticalScroll is not { } saved || !_freezeHint)
            {
                return;
            }

            var bar = _GetVerticalScrollBar();
            if (bar is null || Math.Abs(bar.Value - saved) < 0.5)
            {
                return;
            }

            bar.Value = saved;
        }

        private double? _ReadVerticalScroll()
        {
            return _GetVerticalScrollBar()?.Value;
        }

        private ScrollBar? _GetVerticalScrollBar()
        {
            return RenameGrid
                .GetVisualDescendants()
                .OfType<ScrollBar>()
                .FirstOrDefault(bar => bar.Orientation == Orientation.Vertical);
        }
    }
}
