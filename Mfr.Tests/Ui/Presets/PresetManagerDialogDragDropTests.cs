using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.Tests.Ui.DragAndDrop;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Headless tests for Preset Manager drag-and-drop reorder.
    /// </summary>
    public sealed class PresetManagerDialogDragDropTests
    {
        /// <summary>
        /// Verifies drag-over shows the salmon insert line on the list adorner.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_marks_insert_row()
        {
            var (dialog, _, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");
            var payload = new IndicesDragPayload([0]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.PresetsFormat);

            var firstItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(firstItem);
            var position = firstItem.TranslatePoint(new Point(8, 4), list) ?? new Point(8, 4);

            list.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, list, position, KeyModifiers.None));
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            DropInsertLineAssert.IsVisible(list);

            list.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, list, new Point(-8, -8), KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            DropInsertLineAssert.IsCleared(list);

            dialog.Close();
        }

        /// <summary>
        /// Verifies dropping a selected preset reorders the list and restores selection.
        /// </summary>
        [AvaloniaFact]
        public void Drop_reorders_selected_preset()
        {
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");
            var payload = new IndicesDragPayload([0]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.PresetsFormat);

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
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");
            viewModel.SetSelectedPresets([viewModel.Presets[0], viewModel.Presets[1]]);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var payload = new IndicesDragPayload([0, 1]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.PresetsFormat);

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
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");

            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal(2, viewModel.SelectedPresets.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            list.Focus();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            PresetManagerDialogTestUi.PressListIndex(list, 1);

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
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");

            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal(2, viewModel.SelectedPresets.Count);

            var pressPoint = PresetManagerDialogTestUi.ListIndexClickPoint(dialog, list, 1);
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
    }
}
