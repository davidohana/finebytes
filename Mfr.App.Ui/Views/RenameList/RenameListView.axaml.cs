using System.Collections;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
        private const string DropMarkClass = "drop-mark";

        private RenameListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;
        private DataGridColumn? _lastHintColumn;
        private AddProgressDialog? _addProgressDialog;

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
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, _OnDragOver);
            AddHandler(DragDrop.DragLeaveEvent, _OnDragLeave);
            AddHandler(DragDrop.DropEvent, _OnDrop);
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
                _viewModel.AddProgress.PropertyChanged -= _OnAddProgressPropertyChanged;
            }

            _viewModel = DataContext as RenameListViewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _viewModel.AddProgress.PropertyChanged += _OnAddProgressPropertyChanged;
            _SyncSelectionToGrid();
            _ApplyDropMarkVisuals();
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
                _ClearDropMark();
                return;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                _viewModel.Clear();
                _ClearDropMark();
                e.DragEffects = DragDropEffects.Copy;
                return;
            }

            _UpdateDropMarkFromPointer(e);
            e.DragEffects = DragDropEffects.Copy;
        }

        private void _OnDragLeave(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            _ClearDropMark();
        }

        private async void _OnDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;

            if (_viewModel is null || _viewModel.IsAdding || !_CanAcceptFileDrop(e))
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
                _ClearDropMark();
                return;
            }

            var index = row.Index;
            if (index < 0 || index >= _viewModel.Entries.Count)
            {
                _ClearDropMark();
                return;
            }

            _viewModel.SetDropMarkIndex(index);
        }

        private DataGridRow? _HitTestDataGridRow(DragEventArgs e)
        {
            var position = e.GetPosition(RenameGrid);
            if (RenameGrid.InputHitTest(position) is not Control hit)
            {
                return null;
            }

            return DataGridRow.GetRowContainingElement(hit);
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
