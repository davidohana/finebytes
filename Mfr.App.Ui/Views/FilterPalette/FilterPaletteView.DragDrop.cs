using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters;

namespace Mfr.App.Ui.Views.FilterPalette
{
    public partial class FilterPaletteView
    {
        private const double DragThreshold = 4;

        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private IReadOnlyList<int>? _dragSelectionSnapshot;
        private int? _dragHitIndex;

        private void _WireDragDropHandlers()
        {
            DragDrop.SetAllowDrop(FilterList, true);
            FilterList.AddHandler(PointerPressedEvent, _OnListPointerPressed, RoutingStrategies.Tunnel);
            FilterList.AddHandler(PointerMovedEvent, _OnListPointerMoved, RoutingStrategies.Tunnel);
            FilterList.AddHandler(PointerReleasedEvent, _OnListPointerReleased, RoutingStrategies.Tunnel);
            FilterList.AddHandler(PointerCaptureLostEvent, _OnListPointerCaptureLost, RoutingStrategies.Tunnel);
            FilterList.AddHandler(DragDrop.DragOverEvent, _OnListDragOver);
            FilterList.AddHandler(DragDrop.DropEvent, _OnListDrop);
        }

        private void _OnListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _ClearDragState();

            if (DataContext is not FilterPaletteViewModel || sender is not ListBox listBox)
            {
                return;
            }

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (_FindListBoxItemFromSource(e.Source) is not { } item)
            {
                return;
            }

            var hitIndex = listBox.IndexFromContainer(item);
            var selectedIndices = _ReadSelectedIndices(listBox);
            var isExtendingSelection =
                e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
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

            _dragStartPoint = e.GetPosition(listBox);
            _dragStartArgs = e;
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartArgs is null || _dragStartPoint is null || DataContext is not FilterPaletteViewModel)
            {
                return;
            }

            if (!e.GetCurrentPoint(FilterList).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            var delta = e.GetPosition(FilterList) - _dragStartPoint.Value;
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            {
                return;
            }

            var payload = _BuildDragPayload(FilterList);
            if (payload is null)
            {
                _ClearDragState();
                return;
            }

            var dragArgs = _dragStartArgs;
            _ClearDragState();

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(FilterPaletteDragPayload.Format, payload.Serialize()));

            await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Copy).ConfigureAwait(true);
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_dragSelectionSnapshot is { Count: > 0 } && _dragHitIndex is int hit)
            {
                _RestoreListSelection(FilterList, [hit], hit);
                if (hit >= 0 && hit < FilterList.ItemCount && FilterList.Items[hit] is FilterCatalogEntry entry)
                {
                    if (DataContext is FilterPaletteViewModel viewModel)
                    {
                        viewModel.SelectedFilter = entry;
                    }
                }
            }

            _ClearDragState();
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (AppliedFiltersViewModel is null || _ReadAppliedReorderPayload(e) is null)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            e.Handled = true;
            e.DragEffects = DragDropEffects.Move;
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            if (AppliedFiltersViewModel is null)
            {
                return;
            }

            if (_ReadAppliedReorderPayload(e) is not { } payload)
            {
                return;
            }

            e.Handled = true;
            AppliedFiltersViewModel.RemoveStepsAtIndices(payload.SourceIndices);
        }

        private static AppliedFilterDragPayload? _ReadAppliedReorderPayload(DragEventArgs e)
        {
            if (e.DataTransfer is null)
            {
                return null;
            }

            foreach (var item in e.DataTransfer.Items)
            {
                if (item.TryGetRaw(AppliedFilterDragPayload.Format) is string json)
                {
                    var payload = AppliedFilterDragPayload.Deserialize(json);
                    return payload?.SourceIndices is { Count: > 0 } ? payload : null;
                }
            }

            return null;
        }

        private static FilterPaletteDragPayload? _BuildDragPayload(ListBox listBox)
        {
            var types = listBox
                .Selection.SelectedIndexes.Where(index => index >= 0 && index < listBox.ItemCount)
                .OrderBy(index => index)
                .Select(index => ((FilterCatalogEntry)listBox.Items[index]!).Type)
                .ToList();

            return types.Count == 0 ? null : new FilterPaletteDragPayload(types);
        }

        private void _ClearDragState()
        {
            _dragStartPoint = null;
            _dragStartArgs = null;
            _dragSelectionSnapshot = null;
            _dragHitIndex = null;
        }

        private static ListBoxItem? _FindListBoxItemFromSource(object? source)
        {
            for (var current = source as Visual; current is not null; current = current.GetVisualParent())
            {
                if (current is ListBoxItem item)
                {
                    return item;
                }
            }

            return null;
        }

        private static IReadOnlyList<int> _ReadSelectedIndices(ListBox listBox)
        {
            return [.. listBox.Selection.SelectedIndexes.Where(index => index >= 0).OrderBy(index => index)];
        }

        private static void _RestoreListSelection(ListBox listBox, IReadOnlyList<int> indices, int anchorIndex)
        {
            var itemCount = listBox.ItemCount;
            var desired = indices.Where(index => index >= 0 && index < itemCount).ToList();
            var selection = listBox.Selection;
            selection.BeginBatchUpdate();
            try
            {
                selection.Clear();
                foreach (var index in desired)
                {
                    selection.Select(index);
                }

                if (anchorIndex >= 0 && anchorIndex < itemCount && desired.Contains(anchorIndex))
                {
                    selection.AnchorIndex = anchorIndex;
                    selection.Select(anchorIndex);
                }
            }
            finally
            {
                selection.EndBatchUpdate();
            }
        }
    }
}
