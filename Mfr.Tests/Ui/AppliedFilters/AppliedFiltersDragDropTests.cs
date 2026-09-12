using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.Tests.Ui.DragAndDrop;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for Applied Filters drag-and-drop reorder.
    /// </summary>
    public sealed class AppliedFiltersDragDropTests
    {
        /// <summary>
        /// Verifies drag-over shows the salmon insert line on the list adorner.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_marks_insert_row()
        {
            var (window, _, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            var payload = new IndicesDragPayload([0]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.AppliedFiltersFormat);

            var firstItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(firstItem);
            var position = firstItem.TranslatePoint(new Point(8, 4), list) ?? new Point(8, 4);

            list.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, list, position, KeyModifiers.None));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            DropInsertLineAssert.IsVisible(list);

            list.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, list, new Point(-8, -8), KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            DropInsertLineAssert.IsCleared(list);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a selected row reorders the Applied list.
        /// </summary>
        [AvaloniaFact]
        public void Drop_reorders_selected_filter()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            var payload = new IndicesDragPayload([0]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.AppliedFiltersFormat);

            var targetItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(targetItem);
            var position =
                targetItem.TranslatePoint(new Point(8, targetItem.Bounds.Height - 2), list) ?? new Point(8, 8);

            list.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, dataTransfer, list, position, KeyModifiers.None));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);

            window.Close();
        }

        /// <summary>
        /// Verifies releasing without a drag collapses a multi-selection to the pressed row.
        /// </summary>
        [AvaloniaFact]
        public void Release_without_drag_collapses_multi_selection()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1], list.Selection.SelectedIndexes.OrderBy(index => index).ToList());

            var pressPoint = _ListIndexClickPoint(window, list, 1);
            window.MouseMove(pressPoint, RawInputModifiers.None);
            window.MouseDown(pressPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, viewModel.SelectedSteps.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            window.MouseUp(pressPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([viewModel.Steps[1]], viewModel.SelectedSteps);
            Assert.Equal([1], list.Selection.SelectedIndexes.ToList());

            window.Close();
        }

        private static Point _ListIndexClickPoint(Window window, ListBox list, int index)
        {
            list.ScrollIntoView(index);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = list.ContainerFromIndex(index);
            Assert.NotNull(container);

            var labelText = container
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
            var target = (Visual?)labelText ?? container;
            var local = new Point(Math.Max(8, target.Bounds.Width / 2), Math.Max(1, target.Bounds.Height / 2));
            var windowPoint = target.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }
    }
}
