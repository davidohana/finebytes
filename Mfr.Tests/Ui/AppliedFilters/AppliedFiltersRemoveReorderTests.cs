using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for Applied Filters remove, clear, and reorder gestures.
    /// </summary>
    public sealed class AppliedFiltersRemoveReorderTests
    {
        /// <summary>
        /// Verifies the remove shuttle button deletes the selected step.
        /// </summary>
        [AvaloniaFact]
        public void Remove_button_removes_selected_filter()
        {
            var (window, viewModel, list, view) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);

            var removeButton = view.FindControl<Button>("RemoveFromAppliedButton");
            Assert.NotNull(removeButton);
            Assert.NotNull(removeButton.Command);
            Assert.True(removeButton.Command.CanExecute(null));
            removeButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
            Assert.Equal(1, list.ItemCount);

            window.Close();
        }

        /// <summary>
        /// Verifies Delete on the Applied list removes the selection.
        /// </summary>
        [AvaloniaFact]
        public void Delete_on_applied_list_removes_selected_filter()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            AppliedFiltersTestUi.PressKeyOnControl(list, Key.Delete);

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies Ctrl+Up on the Applied list moves the selection up.
        /// </summary>
        [AvaloniaFact]
        public void Ctrl_up_on_applied_list_moves_selected_filter()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 1);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            AppliedFiltersTestUi.PressKeyOnControl(list, Key.Up, KeyModifiers.Control);

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);

            window.Close();
        }

        /// <summary>
        /// Verifies the move-down shuttle button reorders the selection.
        /// </summary>
        [AvaloniaFact]
        public void Move_down_button_reorders_selected_filter()
        {
            var (window, viewModel, _, view) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);

            var moveDownButton = view.FindControl<Button>("MoveAppliedDownButton");
            Assert.NotNull(moveDownButton);
            Assert.NotNull(moveDownButton.Command);
            Assert.True(moveDownButton.Command.CanExecute(null));
            moveDownButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);

            window.Close();
        }

        /// <summary>
        /// Verifies the clear shuttle button removes every step.
        /// </summary>
        [AvaloniaFact]
        public void Clear_button_removes_all_filters()
        {
            var (window, viewModel, list, view) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);

            var clearButton = view.FindControl<Button>("ClearAppliedButton");
            Assert.NotNull(clearButton);
            Assert.NotNull(clearButton.Command);
            Assert.True(clearButton.Command.CanExecute(null));
            clearButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewModel.Steps);
            Assert.Empty(viewModel.SelectedSteps);
            Assert.Equal(0, list.ItemCount);

            window.Close();
        }
    }
}
