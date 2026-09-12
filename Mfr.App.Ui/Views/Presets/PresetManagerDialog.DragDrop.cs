using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.Presets
{
    public partial class PresetManagerDialog
    {
        private readonly ListBoxDragSession _dragSession = new();
        private readonly ListBoxDropMark _dropMark = new();

        private void _WireDragDropHandlers()
        {
            DragDrop.SetAllowDrop(PresetsList, true);
            PresetsList.AddHandler(PointerPressedEvent, _OnListPointerPressed, RoutingStrategies.Tunnel);
            PresetsList.AddHandler(PointerMovedEvent, _OnListPointerMoved, RoutingStrategies.Tunnel);
            PresetsList.AddHandler(PointerReleasedEvent, _OnListPointerReleased, RoutingStrategies.Tunnel);
            PresetsList.AddHandler(PointerCaptureLostEvent, _OnListPointerCaptureLost, RoutingStrategies.Tunnel);
            PresetsList.AddHandler(DragDrop.DragOverEvent, _OnListDragOver);
            PresetsList.AddHandler(DragDrop.DragLeaveEvent, _OnListDragLeave);
            PresetsList.AddHandler(DragDrop.DropEvent, _OnListDrop);
        }

        private void _OnListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _dragSession.Clear();

            if (_ViewModel is null || sender is not ListBox listBox)
            {
                return;
            }

            _dragSession.Capture(listBox, e);
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_ViewModel is null)
            {
                return;
            }

            await _dragSession
                .TryBeginDragAsync(PresetsList, e, _BuildPresetDrag, _dropMark.Clear)
                .ConfigureAwait(true);
        }

        private ListBoxDragStart? _BuildPresetDrag()
        {
            var indices = ListBoxDrag.ReadSelectedIndices(PresetsList);
            if (indices.Count == 0)
            {
                return null;
            }

            var payload = new IndicesDragPayload(indices);
            return new ListBoxDragStart(payload.CreateTransfer(IndicesDragPayload.PresetsFormat), DragDropEffects.Move);
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Explorer: press on a multi-selected row without dragging collapses to that row on release.
            _dragSession.OnReleased(
                (listBox, _, hit) =>
                {
                    _RestoreListSelection(listBox, [hit], hit);
                    _ViewModel?.SetSelectedPresets(_ReadSelectedPresets(listBox));
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

            if (IndicesDragPayload.TryRead(e.DataTransfer, IndicesDragPayload.PresetsFormat) is null)
            {
                e.DragEffects = DragDropEffects.None;
                _dropMark.Clear();
                return;
            }

            e.Handled = true;
            e.DragEffects = DragDropEffects.Move;
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

            _dropMark.Clear();
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            if (_ViewModel is null || sender is not ListBox targetList)
            {
                return;
            }

            if (IndicesDragPayload.TryRead(e.DataTransfer, IndicesDragPayload.PresetsFormat) is not { } reorderPayload)
            {
                return;
            }

            e.Handled = true;
            _dropMark.Clear();
            var insertIndex = ListBoxDrag.GetDropIndex(targetList, e.GetPosition(targetList));
            _ViewModel.MovePresetsTo(reorderPayload.SourceIndices, insertIndex);
            _QueueRestoreSelectionFromViewModel();
        }
    }
}
