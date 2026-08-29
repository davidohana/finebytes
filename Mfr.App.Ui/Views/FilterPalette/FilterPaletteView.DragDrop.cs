using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters;

namespace Mfr.App.Ui.Views.FilterPalette
{
    public partial class FilterPaletteView
    {
        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;

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

            if (!ListBoxDrag.TryCapturePress(listBox, e, out var press))
            {
                return;
            }

            _dragStartPoint = press.StartPoint;
            _dragStartArgs = press.StartArgs;
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

            if (ListBoxDrag.IsBelowThreshold(_dragStartPoint.Value, e.GetPosition(FilterList)))
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

            var dataTransfer = JsonDragPayload.CreateTransfer(FilterPaletteDragPayload.Format, payload);
            await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Copy).ConfigureAwait(true);
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _ClearDragState();
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _ClearDragState();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (AppliedFiltersViewModel is null || AppliedFilterDragPayload.TryRead(e.DataTransfer) is null)
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

            if (AppliedFilterDragPayload.TryRead(e.DataTransfer) is not { } payload)
            {
                return;
            }

            e.Handled = true;
            AppliedFiltersViewModel.RemoveStepsAtIndices(payload.SourceIndices);
        }

        private static FilterPaletteDragPayload? _BuildDragPayload(ListBox listBox)
        {
            var types = ListBoxDrag
                .ReadSelectedIndices(listBox)
                .Where(index => index < listBox.ItemCount)
                .Select(index => ((FilterCatalogEntry)listBox.Items[index]!).Type)
                .ToList();

            return types.Count == 0 ? null : new FilterPaletteDragPayload(types);
        }

        private void _ClearDragState()
        {
            _dragStartPoint = null;
            _dragStartArgs = null;
        }
    }
}
