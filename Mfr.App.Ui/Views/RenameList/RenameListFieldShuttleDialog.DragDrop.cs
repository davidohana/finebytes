using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    public partial class RenameListFieldShuttleDialog
    {
        private const double DragThreshold = 4;

        private ListBox? _dragSourceList;
        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private IReadOnlyList<int>? _dragSelectionSnapshot;
        private int? _dragHitIndex;
        private ListBoxItem? _dropMarkItem;
        private ListBox? _dropMarkList;
        private int? _dropMarkInsertIndex;
        private Canvas? _appendMarkHost;
        private Rectangle? _appendMarkLine;

        private void _WireDragDropHandlers()
        {
            _WireListDragDrop(AvailableOriginalFieldsList, ShuttleDragKind.AvailableField);
            _WireListDragDrop(AvailablePreviewFieldsList, ShuttleDragKind.AvailableField);
            _WireListDragDrop(AvailableSortFieldsList, ShuttleDragKind.AvailableField);
            _WireListDragDrop(SelectedColumnsList, ShuttleDragKind.SelectedColumn);
            _WireListDragDrop(SelectedSortList, ShuttleDragKind.SelectedSort);
        }

        private void _WireListDragDrop(ListBox listBox, ShuttleDragKind kind)
        {
            DragDrop.SetAllowDrop(listBox, true);
            listBox.AddHandler(PointerPressedEvent, _OnListPointerPressed, RoutingStrategies.Tunnel);
            listBox.AddHandler(PointerMovedEvent, _OnListPointerMoved, RoutingStrategies.Tunnel);
            listBox.AddHandler(PointerReleasedEvent, _OnListPointerReleased, RoutingStrategies.Tunnel);
            listBox.AddHandler(
                PointerCaptureLostEvent,
                _OnListPointerCaptureLost,
                RoutingStrategies.Tunnel
            );
            listBox.AddHandler(DragDrop.DragOverEvent, _OnListDragOver);
            listBox.AddHandler(DragDrop.DragLeaveEvent, _OnListDragLeave);
            listBox.AddHandler(DragDrop.DropEvent, _OnListDrop);
            listBox.Tag = kind;
        }

        private void _OnListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _ClearDragState();

            if (sender is not ListBox listBox || _ViewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (e.Source is not Visual source || source.FindAncestorOfType<ListBoxItem>() is not { } item)
            {
                return;
            }

            var hitIndex = listBox.IndexFromContainer(item);
            var selectedIndices = _ReadSelectedIndices(listBox);
            var isExtendingSelection =
                e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            // Avalonia collapses a multi-selection to the pressed row. Snapshot when pressing an
            // already-selected row so SelectionChanged can undo that before paint (File List pattern).
            if (
                hitIndex >= 0
                && selectedIndices.Count > 1
                && !isExtendingSelection
                && selectedIndices.Contains(hitIndex)
            )
            {
                _dragSelectionSnapshot = selectedIndices;
                _dragHitIndex = hitIndex;
            }

            _dragSourceList = listBox;
            _dragStartPoint = e.GetPosition(listBox);
            _dragStartArgs = e;
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartArgs is null || _dragStartPoint is null || _dragSourceList is null || _ViewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(_dragSourceList).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            var delta = e.GetPosition(_dragSourceList) - _dragStartPoint.Value;
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            {
                return;
            }

            var payload = _BuildDragPayload(_dragSourceList);
            if (payload is null)
            {
                _ClearDragState();
                return;
            }

            var dragArgs = _dragStartArgs;
            _ClearDragState();

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(ShuttleDragPayload.Format, payload.Serialize()));

            var effect = payload.Kind == ShuttleDragKind.AvailableField ? DragDropEffects.Copy : DragDropEffects.Move;
            try
            {
                await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, effect).ConfigureAwait(true);
            }
            finally
            {
                _ClearDropMark();
            }
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Explorer: press on a multi-selected row without dragging collapses to that row on release.
            if (_dragSourceList is not null && _dragSelectionSnapshot is { Count: > 0 } && _dragHitIndex is int hit)
            {
                _RestoreListSelection(_dragSourceList, [hit], hit);
                _SyncViewModelFromListBox(_dragSourceList);
            }

            _ClearDragState();
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (_ViewModel is null || sender is not ListBox targetList)
            {
                e.DragEffects = DragDropEffects.None;
                _ClearDropMark();
                return;
            }

            var payload = _ReadDragPayload(e);
            if (payload is null)
            {
                e.DragEffects = DragDropEffects.None;
                _ClearDropMark();
                return;
            }

            e.Handled = true;
            var effect = _GetDragEffect(payload, targetList);
            e.DragEffects = effect;
            if (effect == DragDropEffects.None || !_IsInsertDropTarget(payload, targetList))
            {
                _ClearDropMark();
                return;
            }

            _UpdateDropMark(targetList, e.GetPosition(targetList));
        }

        private void _OnListDragLeave(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            if (sender is not ListBox listBox)
            {
                return;
            }

            var position = e.GetPosition(listBox);
            if (new Rect(listBox.Bounds.Size).Contains(position))
            {
                return;
            }

            if (ReferenceEquals(listBox, _dropMarkList))
            {
                _ClearDropMark();
            }
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            if (_ViewModel is null || sender is not ListBox targetList)
            {
                return;
            }

            var payload = _ReadDragPayload(e);
            if (payload is null)
            {
                return;
            }

            e.Handled = true;
            var position = e.GetPosition(targetList);
            _ClearDropMark();
            _ApplyDrop(payload, targetList, position);
        }

        private void _ApplyDrop(ShuttleDragPayload payload, ListBox targetList, Point position)
        {
            if (_ViewModel is null)
            {
                return;
            }

            var keys = payload
                .Keys.Select(ShuttleFieldKeyCodec.Decode)
                .Where(key => key.HasValue)
                .Select(key => key!.Value)
                .ToList();

            if (keys.Count == 0)
            {
                return;
            }

            if (payload.Kind == ShuttleDragKind.AvailableField)
            {
                if (ReferenceEquals(targetList, SelectedColumnsList))
                {
                    _ViewModel.InsertColumnsAt(keys, _GetDropIndex(SelectedColumnsList, position));
                    return;
                }

                if (ReferenceEquals(targetList, SelectedSortList))
                {
                    var sortKeys = keys.Where(key => !key.IsPreview).ToList();
                    _ViewModel.InsertSortKeysAt(sortKeys, _GetDropIndex(SelectedSortList, position));
                }

                return;
            }

            if (payload.Kind == ShuttleDragKind.SelectedColumn)
            {
                if (
                    ReferenceEquals(targetList, AvailableOriginalFieldsList)
                    || ReferenceEquals(targetList, AvailablePreviewFieldsList)
                )
                {
                    var indices = _IndicesForColumnKeys(keys);
                    _ViewModel.RemoveColumnsAtIndices(indices);
                    return;
                }

                if (ReferenceEquals(targetList, SelectedColumnsList))
                {
                    var sourceIndices = _IndicesForColumnKeys(keys);
                    _ViewModel.MoveColumnsTo(sourceIndices, _GetDropIndex(SelectedColumnsList, position));
                }

                return;
            }

            if (payload.Kind == ShuttleDragKind.SelectedSort)
            {
                if (ReferenceEquals(targetList, AvailableSortFieldsList))
                {
                    var indices = _IndicesForSortKeys(keys);
                    _ViewModel.RemoveSortKeysAtIndices(indices);
                    return;
                }

                if (ReferenceEquals(targetList, SelectedSortList))
                {
                    var sourceIndices = _IndicesForSortKeys(keys);
                    _ViewModel.MoveSortKeysTo(sourceIndices, _GetDropIndex(SelectedSortList, position));
                }
            }
        }

        private ShuttleDragPayload? _BuildDragPayload(ListBox sourceList)
        {
            if (ReferenceEquals(sourceList, AvailableOriginalFieldsList))
            {
                return _BuildAvailablePayload(_ReadSelectedFields(AvailableOriginalFieldsList), usePreviewKeys: false);
            }

            if (ReferenceEquals(sourceList, AvailablePreviewFieldsList))
            {
                return _BuildAvailablePayload(_ReadSelectedFields(AvailablePreviewFieldsList), usePreviewKeys: true);
            }

            if (ReferenceEquals(sourceList, AvailableSortFieldsList))
            {
                return _BuildAvailablePayload(_ReadSelectedFields(AvailableSortFieldsList), usePreviewKeys: false);
            }

            if (ReferenceEquals(sourceList, SelectedColumnsList))
            {
                var rows = _ReadSelectedColumnRows();
                return new ShuttleDragPayload(
                    ShuttleDragKind.SelectedColumn,
                    [.. rows.Select(row => ShuttleFieldKeyCodec.Encode(row.Column.Key))]
                );
            }

            if (ReferenceEquals(sourceList, SelectedSortList))
            {
                var rows = _ReadSelectedSortRows();
                return new ShuttleDragPayload(
                    ShuttleDragKind.SelectedSort,
                    [.. rows.Select(row => ShuttleFieldKeyCodec.Encode(row.Key.FieldKey))]
                );
            }

            return null;
        }

        private static ShuttleDragPayload? _BuildAvailablePayload(
            IReadOnlyList<RenameListField> fields,
            bool usePreviewKeys
        )
        {
            if (fields.Count == 0)
            {
                return null;
            }

            var encodedKeys = fields
                .Select(field => ShuttleFieldKeyCodec.Encode(usePreviewKeys ? field.PreviewKey : field.OriginalKey))
                .ToList();

            return new ShuttleDragPayload(ShuttleDragKind.AvailableField, encodedKeys);
        }

        private IReadOnlyList<RenameListFieldShuttleColumnRow> _ReadSelectedColumnRows()
        {
            if (_ViewModel is null)
            {
                return [];
            }

            var rows = _ViewModel.SelectedColumnRows;
            return
            [
                .. _ReadSelectedIndices(SelectedColumnsList)
                    .Where(index => index < rows.Count)
                    .Select(index => rows[index]),
            ];
        }

        private IReadOnlyList<RenameListFieldShuttleSortRow> _ReadSelectedSortRows()
        {
            if (_ViewModel is null)
            {
                return [];
            }

            var rows = _ViewModel.SelectedSortRows;
            return
            [
                .. _ReadSelectedIndices(SelectedSortList)
                    .Where(index => index < rows.Count)
                    .Select(index => rows[index]),
            ];
        }

        private IReadOnlyList<int> _IndicesForColumnKeys(IReadOnlyList<RenameListFieldKey> keys)
        {
            if (_ViewModel is null)
            {
                return [];
            }

            var keySet = keys.ToHashSet();
            return [.. _ViewModel
                .SelectedColumnRows.Where(row => keySet.Contains(row.Column.Key))
                .Select(row => row.Index)
                .OrderBy(index => index)];
        }

        private IReadOnlyList<int> _IndicesForSortKeys(IReadOnlyList<RenameListFieldKey> keys)
        {
            if (_ViewModel is null)
            {
                return [];
            }

            var keySet = keys.ToHashSet();
            return [.. _ViewModel
                .SelectedSortRows.Where(row => keySet.Contains(row.Key.FieldKey))
                .Select(row => row.Index)
                .OrderBy(index => index)];
        }

        private static int _GetDropIndex(ListBox listBox, Point position)
        {
            var item = _HitTestListBoxItem(listBox, position);
            if (item is null)
            {
                return listBox.ItemCount;
            }

            var itemIndex = listBox.IndexFromContainer(item);
            if (itemIndex < 0)
            {
                return listBox.ItemCount;
            }

            var itemOrigin = item.TranslatePoint(default, listBox);
            if (itemOrigin is null)
            {
                return itemIndex;
            }

            var midpoint = itemOrigin.Value.Y + (item.Bounds.Height / 2);
            return position.Y >= midpoint ? itemIndex + 1 : itemIndex;
        }

        private static ListBoxItem? _HitTestListBoxItem(ListBox listBox, Point position)
        {
            foreach (var item in listBox.GetVisualDescendants().OfType<ListBoxItem>())
            {
                var origin = item.TranslatePoint(default, listBox);
                if (origin is null)
                {
                    continue;
                }

                if (new Rect(origin.Value, item.Bounds.Size).Contains(position))
                {
                    return item;
                }
            }

            return null;
        }

        private static ShuttleDragPayload? _ReadDragPayload(DragEventArgs e)
        {
            if (e.DataTransfer is null)
            {
                return null;
            }

            foreach (var item in e.DataTransfer.Items)
            {
                if (item.TryGetRaw(ShuttleDragPayload.Format) is string json)
                {
                    return ShuttleDragPayload.Deserialize(json);
                }
            }

            return null;
        }

        private bool _IsInsertDropTarget(ShuttleDragPayload payload, ListBox targetList)
        {
            return payload.Kind switch
            {
                ShuttleDragKind.AvailableField
                    when ReferenceEquals(targetList, SelectedColumnsList)
                        || ReferenceEquals(targetList, SelectedSortList) => true,
                ShuttleDragKind.SelectedColumn when ReferenceEquals(targetList, SelectedColumnsList) => true,
                ShuttleDragKind.SelectedSort when ReferenceEquals(targetList, SelectedSortList) => true,
                _ => false,
            };
        }

        /// <summary>
        /// Shows the salmon insert marker at the drop index: row highlight, or a line after the last item.
        /// </summary>
        private void _UpdateDropMark(ListBox listBox, Point position)
        {
            var insertIndex = _GetDropIndex(listBox, position);
            if (_HitTestListBoxItem(listBox, position) is null && ReferenceEquals(listBox, _dropMarkList))
            {
                return;
            }

            if (ReferenceEquals(listBox, _dropMarkList) && _dropMarkInsertIndex == insertIndex)
            {
                return;
            }

            _ClearDropMark();
            _dropMarkList = listBox;
            _dropMarkInsertIndex = insertIndex;

            if (insertIndex < listBox.ItemCount)
            {
                var item = listBox.ContainerFromIndex(insertIndex) as ListBoxItem;
                if (item is not null)
                {
                    item.Classes.Set("drop-mark", true);
                    _dropMarkItem = item;
                }

                return;
            }

            _ShowAppendMark(listBox);
        }

        private void _ShowAppendMark(ListBox listBox)
        {
            var y = 2.0;
            if (
                listBox.ItemCount > 0
                && listBox.ContainerFromIndex(listBox.ItemCount - 1) is Control lastItem
                && lastItem.TranslatePoint(default, listBox) is { } origin
            )
            {
                y = origin.Y + lastItem.Bounds.Height;
            }

            _appendMarkHost ??= new Canvas { IsHitTestVisible = false };
            _appendMarkLine ??= new Rectangle
            {
                Height = 3,
                IsHitTestVisible = false,
                Fill = _DropMarkBrush(),
            };

            if (_appendMarkLine.Parent is null)
            {
                _appendMarkHost.Children.Add(_appendMarkLine);
            }

            _appendMarkLine.Width = Math.Max(0, listBox.Bounds.Width - 4);
            Canvas.SetLeft(_appendMarkLine, 2);
            Canvas.SetTop(_appendMarkLine, Math.Clamp(y - 1.5, 0, Math.Max(0, listBox.Bounds.Height - 3)));
            AdornerLayer.SetAdorner(listBox, _appendMarkHost);
        }

        private IBrush _DropMarkBrush()
        {
            if (
                TryGetResource("RenameListDropIndicatorBrush", ActualThemeVariant, out var resource)
                && resource is IBrush brush
            )
            {
                return brush;
            }

            return new SolidColorBrush(Color.Parse("#FA8072"));
        }

        private void _ClearDropMark()
        {
            _dropMarkItem?.Classes.Set("drop-mark", false);
            _dropMarkItem = null;

            if (_dropMarkList is { } markedList)
            {
                AdornerLayer.SetAdorner(markedList, null);
            }

            _dropMarkList = null;
            _dropMarkInsertIndex = null;
        }

        private DragDropEffects _GetDragEffect(ShuttleDragPayload payload, ListBox targetList)
        {
            return payload.Kind switch
            {
                ShuttleDragKind.AvailableField
                    when ReferenceEquals(targetList, SelectedColumnsList)
                        || ReferenceEquals(targetList, SelectedSortList) => DragDropEffects.Copy,
                ShuttleDragKind.SelectedColumn
                    when ReferenceEquals(targetList, AvailableOriginalFieldsList)
                        || ReferenceEquals(targetList, AvailablePreviewFieldsList) => DragDropEffects.Move,
                ShuttleDragKind.SelectedColumn when ReferenceEquals(targetList, SelectedColumnsList) =>
                    DragDropEffects.Move,
                ShuttleDragKind.SelectedSort when ReferenceEquals(targetList, AvailableSortFieldsList) =>
                    DragDropEffects.Move,
                ShuttleDragKind.SelectedSort when ReferenceEquals(targetList, SelectedSortList) => DragDropEffects.Move,
                _ => DragDropEffects.None,
            };
        }

        private void _ClearDragState()
        {
            _dragSourceList = null;
            _dragStartPoint = null;
            _dragStartArgs = null;
            _dragSelectionSnapshot = null;
            _dragHitIndex = null;
        }
    }
}
