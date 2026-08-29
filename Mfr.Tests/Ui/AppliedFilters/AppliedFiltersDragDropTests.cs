using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.Views.AppliedFilters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for Applied Filters drag-and-drop reorder.
    /// </summary>
    public sealed class AppliedFiltersDragDropTests
    {
        /// <summary>
        /// Verifies drag-over paints the salmon insert marker on the target row.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_marks_insert_row()
        {
            var (window, _, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            var payload = new AppliedFilterDragPayload([0]);
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(AppliedFilterDragPayload.Format, payload.Serialize()));

            var firstItem = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(firstItem);
            var position = firstItem.TranslatePoint(new Point(8, 4), list) ?? new Point(8, 4);

            list.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, list, position, KeyModifiers.None));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("drop-mark", firstItem.Classes);

            list.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, list, new Point(-8, -8), KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.DoesNotContain("drop-mark", firstItem.Classes);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a selected row reorders the Applied list.
        /// </summary>
        [AvaloniaFact]
        public void Drop_reorders_selected_filter()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            var payload = new AppliedFilterDragPayload([0]);
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(AppliedFilterDragPayload.Format, payload.Serialize()));

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
    }
}
