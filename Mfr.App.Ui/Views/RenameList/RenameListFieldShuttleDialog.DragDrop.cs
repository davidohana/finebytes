using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    public partial class RenameListFieldShuttleDialog
    {
        private readonly ListBoxDragSession _dragSession = new();
        private readonly ListBoxDropMark _dropMark = new();

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
            listBox.AddHandler(PointerCaptureLostEvent, _OnListPointerCaptureLost, RoutingStrategies.Tunnel);
            listBox.AddHandler(DragDrop.DragOverEvent, _OnListDragOver);
            listBox.AddHandler(DragDrop.DragLeaveEvent, _OnListDragLeave);
            listBox.AddHandler(DragDrop.DropEvent, _OnListDrop);
            listBox.Tag = kind;
        }

        private void _OnListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _dragSession.Clear();

            if (sender is not ListBox listBox || _ViewModel is null)
            {
                return;
            }

            _dragSession.Capture(listBox, e);
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_ViewModel is null || _dragSession.SourceList is not ListBox sourceList)
            {
                return;
            }

            await _dragSession
                .TryBeginDragAsync(sourceList, e, _BuildShuttleDrag, _dropMark.Clear)
                .ConfigureAwait(true);
        }

        private ListBoxDragStart? _BuildShuttleDrag()
        {
            if (_dragSession.SourceList is not ListBox sourceList)
            {
                return null;
            }

            var payload = _BuildDragPayload(sourceList);
            if (payload is null)
            {
                return null;
            }

            var effect = payload.Kind == ShuttleDragKind.AvailableField ? DragDropEffects.Copy : DragDropEffects.Move;
            return new ListBoxDragStart(JsonDragPayload.CreateTransfer(ShuttleDragPayload.Format, payload), effect);
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Explorer: press on a multi-selected row without dragging collapses to that row on release.
            _dragSession.OnReleased(
                (listBox, _, hit) =>
                {
                    _RestoreListSelection(listBox, [hit], hit);
                    _SyncViewModelFromListBox(listBox);
                }
            );
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _dragSession.Clear();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (_ViewModel is null || sender is not ListBox targetList)
            {
                e.DragEffects = DragDropEffects.None;
                _dropMark.Clear();
                return;
            }

            var payload = ShuttleDragPayload.TryRead(e.DataTransfer);
            if (payload is null)
            {
                e.DragEffects = DragDropEffects.None;
                _dropMark.Clear();
                return;
            }

            e.Handled = true;
            var effect = _GetDragEffect(payload, targetList);
            e.DragEffects = effect;
            if (effect == DragDropEffects.None || !_IsInsertDropTarget(payload, targetList))
            {
                _dropMark.Clear();
                return;
            }

            _dropMark.Update(targetList, e.GetPosition(targetList));
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

            _dropMark.ClearIfHost(listBox);
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            if (_ViewModel is null || sender is not ListBox targetList)
            {
                return;
            }

            var payload = ShuttleDragPayload.TryRead(e.DataTransfer);
            if (payload is null)
            {
                return;
            }

            e.Handled = true;
            var position = e.GetPosition(targetList);
            _dropMark.Clear();
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
                    _ViewModel.InsertColumnsAt(keys, ListBoxDrag.GetDropIndex(SelectedColumnsList, position));
                    return;
                }

                if (ReferenceEquals(targetList, SelectedSortList))
                {
                    var sortKeys = keys.Where(key => !key.IsPreview).ToList();
                    _ViewModel.InsertSortKeysAt(sortKeys, ListBoxDrag.GetDropIndex(SelectedSortList, position));
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
                    _ViewModel.MoveColumnsTo(sourceIndices, ListBoxDrag.GetDropIndex(SelectedColumnsList, position));
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
                    _ViewModel.MoveSortKeysTo(sourceIndices, ListBoxDrag.GetDropIndex(SelectedSortList, position));
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
            return
            [
                .. _ViewModel
                    .SelectedColumnRows.Where(row => keySet.Contains(row.Column.Key))
                    .Select(row => row.Index)
                    .OrderBy(index => index),
            ];
        }

        private IReadOnlyList<int> _IndicesForSortKeys(IReadOnlyList<RenameListFieldKey> keys)
        {
            if (_ViewModel is null)
            {
                return [];
            }

            var keySet = keys.ToHashSet();
            return
            [
                .. _ViewModel
                    .SelectedSortRows.Where(row => keySet.Contains(row.Key.FieldKey))
                    .Select(row => row.Index)
                    .OrderBy(index => index),
            ];
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
    }
}
