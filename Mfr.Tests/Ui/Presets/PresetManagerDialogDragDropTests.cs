using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Headless tests for Preset Manager drag-and-drop reorder.
    /// </summary>
    public sealed class PresetManagerDialogDragDropTests
    {
        /// <summary>
        /// Verifies drag-over paints the salmon insert marker on the target row.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_marks_insert_row()
        {
            var (dialog, _, list) = _ShowWithPresets("A", "B", "C");
            var payload = new PresetDragPayload([0]);
            var dataTransfer = payload.CreateTransfer();

            var firstItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(firstItem);
            var position = firstItem.TranslatePoint(new Point(8, 4), list) ?? new Point(8, 4);

            list.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, list, position, KeyModifiers.None));
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("drop-mark", firstItem.Classes);

            list.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, list, new Point(-8, -8), KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.DoesNotContain("drop-mark", firstItem.Classes);

            dialog.Close();
        }

        /// <summary>
        /// Verifies dropping a selected preset reorders the list and restores selection.
        /// </summary>
        [AvaloniaFact]
        public void Drop_reorders_selected_preset()
        {
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");
            var payload = new PresetDragPayload([0]);
            var dataTransfer = payload.CreateTransfer();

            var targetItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(targetItem);
            var position =
                targetItem.TranslatePoint(new Point(8, targetItem.Bounds.Height - 2), list) ?? new Point(8, 8);

            list.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, dataTransfer, list, position, KeyModifiers.None));
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["B", "A", "C"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([1], list.Selection.SelectedIndexes.ToList());

            dialog.Close();
        }

        /// <summary>
        /// Verifies dropping a multi-selection moves the block and keeps those rows selected.
        /// </summary>
        [AvaloniaFact]
        public void Drop_moves_multi_selected_block()
        {
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");
            viewModel.SetSelectedPresets([viewModel.Presets[0], viewModel.Presets[1]]);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var payload = new PresetDragPayload([0, 1]);
            var dataTransfer = payload.CreateTransfer();

            var targetItem = list.ContainerFromIndex(2) as ListBoxItem;
            Assert.NotNull(targetItem);
            var position =
                targetItem.TranslatePoint(new Point(8, targetItem.Bounds.Height - 2), list) ?? new Point(8, 8);

            list.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, dataTransfer, list, position, KeyModifiers.None));
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["C", "A", "B"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([1, 2], list.Selection.SelectedIndexes.OrderBy(index => index).ToList());

            dialog.Close();
        }

        /// <summary>
        /// Verifies pressing a row in a multi-selection keeps the full selection before drag starts.
        /// </summary>
        [AvaloniaFact]
        public void Press_on_multi_selected_preset_keeps_selection()
        {
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal(2, viewModel.SelectedPresets.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            list.Focus();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            _PressListIndex(list, 1);

            Assert.Equal(2, viewModel.SelectedPresets.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));

            dialog.Close();
        }

        /// <summary>
        /// Verifies releasing without a drag collapses a multi-selection to the pressed row.
        /// </summary>
        [AvaloniaFact]
        public void Release_without_drag_collapses_multi_selection()
        {
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal(2, viewModel.SelectedPresets.Count);

            var pressPoint = _ListIndexClickPoint(dialog, list, 1);
            dialog.MouseMove(pressPoint, RawInputModifiers.None);
            dialog.MouseDown(pressPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, viewModel.SelectedPresets.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            dialog.MouseUp(pressPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([1], list.Selection.SelectedIndexes.ToList());

            dialog.Close();
        }

        private static (
            PresetManagerDialog Dialog,
            PresetManagerDialogViewModel ViewModel,
            ListBox List
        ) _ShowWithPresets(params string[] names)
        {
            var manager = PresetManager.CreateEmpty();
            foreach (var name in names)
            {
                manager.Upsert(
                    new FilterPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = $"desc-{name}",
                        Chain = new FilterChain { Steps = [] },
                    }
                );
            }

            var appliedFilters = new AppliedFiltersViewModel(presetManager: manager);
            var viewModel = new PresetManagerDialogViewModel(appliedFilters);
            var dialog = new PresetManagerDialog(viewModel, appliedFilters, tryLoadAsync: _ => Task.FromResult(false));
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("PresetsList");
            Assert.NotNull(list);
            return (dialog, viewModel, list);
        }

        private static void _ClickListIndex(
            PresetManagerDialog dialog,
            ListBox list,
            int index,
            RawInputModifiers modifiers
        )
        {
            var windowPoint = _ListIndexClickPoint(dialog, list, index);
            dialog.MouseMove(windowPoint, modifiers);
            dialog.MouseDown(windowPoint, MouseButton.Left, modifiers);
            dialog.MouseUp(windowPoint, MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }

        private static void _PressListIndex(ListBox list, int index)
        {
            var item = list.ContainerFromIndex(index) as ListBoxItem;
            Assert.NotNull(item);

            var point = new Point(8, 4);
            var props = new PointerPointProperties(
                RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed
            );
            var pointer = new Pointer(1, PointerType.Mouse, true);
            list.RaiseEvent(
                new PointerPressedEventArgs(item, pointer, list, point, 0, props, KeyModifiers.None, clickCount: 1)
                {
                    RoutedEvent = InputElement.PointerPressedEvent,
                }
            );
            Dispatcher.UIThread.RunJobs();
        }

        private static Point _ListIndexClickPoint(PresetManagerDialog dialog, ListBox list, int index)
        {
            var container = list.ContainerFromIndex(index) as Visual;
            Assert.NotNull(container);

            var labelText = container
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
            var target = (Visual?)labelText ?? container;
            var local = new Point(Math.Max(8, target.Bounds.Width / 2), Math.Max(1, target.Bounds.Height / 2));
            var windowPoint = target.TranslatePoint(local, dialog);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }
    }
}
