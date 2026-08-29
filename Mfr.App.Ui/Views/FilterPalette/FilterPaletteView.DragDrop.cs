using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.Filters;

namespace Mfr.App.Ui.Views.FilterPalette
{
    public partial class FilterPaletteView
    {
        private readonly ListBoxDragSession _dragSession = new();

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
            _dragSession.Clear();

            if (DataContext is not FilterPaletteViewModel || sender is not ListBox listBox)
            {
                return;
            }

            _dragSession.Capture(listBox, e);
        }

        private async void _OnListPointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is not FilterPaletteViewModel)
            {
                return;
            }

            await _dragSession.TryBeginDragAsync(FilterList, e, _BuildPaletteDrag).ConfigureAwait(true);
        }

        private ListBoxDragStart? _BuildPaletteDrag()
        {
            var payload = _BuildDragPayload(FilterList);
            if (payload is null)
            {
                return null;
            }

            return new ListBoxDragStart(
                JsonDragPayload.CreateTransfer(FilterPaletteDragPayload.Format, payload),
                DragDropEffects.Copy
            );
        }

        private void _OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragSession.OnReleased();
        }

        private void _OnListPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _dragSession.Clear();
        }

        private void _OnListDragOver(object? sender, DragEventArgs e)
        {
            if (RemoveAppliedStepsCommand is null || AppliedFilterDragPayload.TryRead(e.DataTransfer) is null)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            e.Handled = true;
            e.DragEffects = DragDropEffects.Move;
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            var command = RemoveAppliedStepsCommand;
            if (command is null || AppliedFilterDragPayload.TryRead(e.DataTransfer) is not { } payload)
            {
                return;
            }

            e.Handled = true;
            if (command.CanExecute(payload.SourceIndices))
            {
                command.Execute(payload.SourceIndices);
            }
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
    }
}
