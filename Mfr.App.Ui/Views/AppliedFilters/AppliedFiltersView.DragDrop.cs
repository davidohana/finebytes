using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.Filters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private const double DragThreshold = 4;

        private Point? _dragStartPoint;
        private PointerEventArgs? _dragStartArgs;
        private IReadOnlyList<int>? _dragSelectionSnapshot;
        private int? _dragHitIndex;
        private ListBoxItem? _dropMarkItem;
        private int? _dropMarkInsertIndex;
        private Canvas? _appendMarkHost;
        private Rectangle? _appendMarkLine;

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
            if (_dragStartArgs is null || _dragStartPoint is null || _viewModel is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(AppliedFiltersList).Properties.IsLeftButtonPressed)
            {
                _ClearDragState();
                return;
            }

            var delta = e.GetPosition(AppliedFiltersList) - _dragStartPoint.Value;
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            {
                return;
            }

            var indices = _ReadSelectedIndices(AppliedFiltersList);
            if (indices.Count == 0)
            {
                _ClearDragState();
                return;
            }

            var payload = new AppliedFilterDragPayload(indices);
            var dragArgs = _dragStartArgs;
            _ClearDragState();

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(AppliedFilterDragPayload.Format, payload.Serialize()));

            try
            {
                await DragDrop.DoDragDropAsync(dragArgs, dataTransfer, DragDropEffects.Move).ConfigureAwait(true);
            }
            finally
            {
                _ClearDropMark();
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
                _ClearDropMark();
                return;
            }

            var isFromPalette = _ReadPalettePayload(e) is not null;
            var isReorder = !isFromPalette && _ReadReorderPayload(e) is not null;
            if (!isReorder && !isFromPalette)
            {
                e.DragEffects = DragDropEffects.None;
                _ClearDropMark();
                return;
            }

            e.Handled = true;
            e.DragEffects = isFromPalette ? DragDropEffects.Copy : DragDropEffects.Move;
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

            _ClearDropMark();
        }

        private void _OnListDrop(object? sender, DragEventArgs e)
        {
            if (_viewModel is null || sender is not ListBox targetList)
            {
                return;
            }

            var position = e.GetPosition(targetList);
            var insertIndex = _GetDropIndex(targetList, position);

            if (_ReadPalettePayload(e) is { } palettePayload)
            {
                var entries = _ResolveCatalogEntries(palettePayload);
                if (entries.Count == 0)
                {
                    return;
                }

                e.Handled = true;
                _ClearDropMark();
                _viewModel.InsertFromCatalogAt(entries, insertIndex);
                _QueueRestoreSelectionFromViewModel();
                return;
            }

            if (_ReadReorderPayload(e) is { } reorderPayload)
            {
                e.Handled = true;
                _ClearDropMark();
                _viewModel.MoveStepsTo(reorderPayload.SourceIndices, insertIndex);
                _QueueRestoreSelectionFromViewModel();
            }
        }

        private static AppliedFilterDragPayload? _ReadReorderPayload(DragEventArgs e)
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

        private static FilterPaletteDragPayload? _ReadPalettePayload(DragEventArgs e)
        {
            if (e.DataTransfer is null)
            {
                return null;
            }

            foreach (var item in e.DataTransfer.Items)
            {
                if (item.TryGetRaw(FilterPaletteDragPayload.Format) is string json)
                {
                    var payload = FilterPaletteDragPayload.Deserialize(json);
                    return payload?.CatalogTypes is { Count: > 0 } ? payload : null;
                }
            }

            return null;
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

        private void _UpdateDropMark(ListBox listBox, Point position)
        {
            var insertIndex = _GetDropIndex(listBox, position);
            if (_HitTestListBoxItem(listBox, position) is null && _dropMarkInsertIndex == insertIndex)
            {
                return;
            }

            if (_dropMarkInsertIndex == insertIndex)
            {
                return;
            }

            _ClearDropMark();
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
            AdornerLayer.SetAdorner(AppliedFiltersList, null);
            _dropMarkInsertIndex = null;
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
    }
}
