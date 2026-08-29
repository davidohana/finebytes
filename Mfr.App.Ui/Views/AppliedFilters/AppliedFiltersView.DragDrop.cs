using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.Filters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private readonly ListBoxDragSession _dragSession = new();
        private readonly ListBoxDropMark _dropMark = new();

        private void _WireDragDropHandlers()
        {
            DragDrop.SetAllowDrop(AppliedFiltersList, true);
            AppliedFiltersList.AddHandler(PointerPressedEvent, _OnListPointerPressed, RoutingStrategies.Tunnel);
            AppliedFiltersList.AddHandler(PointerMovedEvent, _OnListPointerMoved, RoutingStrategies.Tunnel);
            AppliedFiltersList.AddHandler(PointerReleasedEvent, _OnListPointerReleased, RoutingStrategies.Tunnel);
            AppliedFiltersList.AddHandler(PointerCaptureLostEvent, _OnListPointerCaptureLost, RoutingStrategies.Tunnel);
            AppliedFiltersList.AddHandler(DragDrop.DragOverEvent, _OnListDragOver);
            AppliedFiltersList.AddHandler(DragDrop.DragLeaveEvent, _OnListDragLeave);
            AppliedFiltersList.AddHandler(DragDrop.DropEvent, _OnListDrop);
        }

        private void _OnListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _dragSession.Clear();

            if (_viewModel is null || sender is not ListBox listBox)
            {
                return;
            }

            _dragSession.Capture(listBox, e);
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            await _dragSession
                .TryBeginDragAsync(AppliedFiltersList, e, _BuildAppliedFilterDrag, _dropMark.Clear)
                .ConfigureAwait(true);
        }

        private ListBoxDragStart? _BuildAppliedFilterDrag()
        {
            var indices = ListBoxDrag.ReadSelectedIndices(AppliedFiltersList);
            if (indices.Count == 0)
            {
                return null;
            }

            var payload = new AppliedFilterDragPayload(indices);
            return new ListBoxDragStart(
                JsonDragPayload.CreateTransfer(AppliedFilterDragPayload.Format, payload),
                DragDropEffects.Move
            );
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragSession.OnReleased(
                (listBox, snapshot, hit) =>
                {
                    _RestoreListSelection(listBox, snapshot, hit);
                    _viewModel?.SetSelectedSteps(_ReadSelectedSteps(listBox));
                }
            );
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _dragSession.Clear();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (_viewModel is null || sender is not ListBox targetList)
            {
                e.DragEffects = DragDropEffects.None;
                _dropMark.Clear();
                return;
            }

            var isFromPalette = FilterPaletteDragPayload.TryRead(e.DataTransfer) is not null;
            var isReorder = !isFromPalette && AppliedFilterDragPayload.TryRead(e.DataTransfer) is not null;
            if (!isReorder && !isFromPalette)
            {
                e.DragEffects = DragDropEffects.None;
                _dropMark.Clear();
                return;
            }

            e.Handled = true;
            e.DragEffects = isFromPalette ? DragDropEffects.Copy : DragDropEffects.Move;
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
            if (_viewModel is null || sender is not ListBox targetList)
            {
                return;
            }

            var position = e.GetPosition(targetList);
            var insertIndex = ListBoxDrag.GetDropIndex(targetList, position);

            if (FilterPaletteDragPayload.TryRead(e.DataTransfer) is { } palettePayload)
            {
                var entries = _ResolveCatalogEntries(palettePayload);
                e.Handled = true;
                _dropMark.Clear();
                if (entries.Count == 0)
                {
                    return;
                }

                _viewModel.InsertFromCatalogAt(entries, insertIndex);
                _QueueRestoreSelectionFromViewModel();
                return;
            }

            if (AppliedFilterDragPayload.TryRead(e.DataTransfer) is { } reorderPayload)
            {
                e.Handled = true;
                _dropMark.Clear();
                _viewModel.MoveStepsTo(reorderPayload.SourceIndices, insertIndex);
                _QueueRestoreSelectionFromViewModel();
            }
        }

        private static List<FilterCatalogEntry> _ResolveCatalogEntries(FilterPaletteDragPayload payload)
        {
            var typeToEntry = FilterCatalog.Entries.ToDictionary(entry => entry.Type, StringComparer.Ordinal);
            var resolved = new List<FilterCatalogEntry>();
            foreach (var type in payload.CatalogTypes)
            {
                if (typeToEntry.TryGetValue(type, out var entry))
                {
                    resolved.Add(entry);
                }
            }

            return resolved;
        }
    }
}
