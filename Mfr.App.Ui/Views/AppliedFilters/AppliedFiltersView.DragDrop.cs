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
        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private IReadOnlyList<int>? _dragSelectionSnapshot;
        private int? _dragHitIndex;
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
            _ClearDragState();

            if (_viewModel is null || sender is not ListBox listBox)
            {
                return;
            }

            if (!ListBoxDrag.TryCapturePress(listBox, e, out var press))
            {
                return;
            }

            _dragSelectionSnapshot = press.SelectionSnapshot;
            _dragHitIndex = press.HitIndex;
            _dragStartPoint = press.StartPoint;
            _dragStartArgs = press.StartArgs;
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartArgs is null || _dragStartPoint is null || _viewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(AppliedFiltersList).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            if (ListBoxDrag.IsBelowThreshold(_dragStartPoint.Value, e.GetPosition(AppliedFiltersList)))
            {
                return;
            }

            var indices = ListBoxDrag.ReadSelectedIndices(AppliedFiltersList);
            if (indices.Count == 0)
            {
                _ClearDragState();
                return;
            }

            var payload = new AppliedFilterDragPayload(indices);
            var dragArgs = _dragStartArgs;
            _ClearDragState();

            var dataTransfer = JsonDragPayload.CreateTransfer(AppliedFilterDragPayload.Format, payload);

            try
            {
                await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Move).ConfigureAwait(true);
            }
            finally
            {
                _dropMark.Clear();
            }
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_dragSelectionSnapshot is { Count: > 0 } && _dragHitIndex is int hit)
            {
                _RestoreListSelection(AppliedFiltersList, _dragSelectionSnapshot, hit);
                _viewModel?.SetSelectedSteps(_ReadSelectedSteps(AppliedFiltersList));
            }

            _ClearDragState();
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
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

        private void _ClearDragState()
        {
            _dragStartPoint = null;
            _dragStartArgs = null;
            _dragSelectionSnapshot = null;
            _dragHitIndex = null;
        }
    }
}
