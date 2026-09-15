using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.FilterChainPane;

namespace Mfr.Tests.Ui.FilterChainPane
{
    /// <summary>
    /// Headless tests for Filter Chain remove, clear, and reorder gestures.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FilterChainRemoveReorderTests
    {
        /// <summary>
        /// Initializes a fresh empty config so clear confirmation is isolated.
        /// </summary>
        public FilterChainRemoveReorderTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies the remove shuttle button deletes the selected step.
        /// </summary>
        [AvaloniaFact]
        public void Remove_button_removes_selected_filter()
        {
            var (window, viewModel, list, view) = FilterChainTestUi.ShowSeededList(selectIndex: 0);

            var removeButton = view.FindControl<Button>("RemoveFromFilterChainButton");
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
        /// Verifies Delete on the Filter Chain list removes the selection.
        /// </summary>
        [AvaloniaFact]
        public void Delete_on_filter_chain_list_removes_selected_filter()
        {
            var (window, viewModel, list, _) = FilterChainTestUi.ShowSeededList(selectIndex: 0);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            FilterChainTestUi.PressKeyOnControl(list, Key.Delete);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies Ctrl+Up on the Filter Chain list moves the selection up.
        /// </summary>
        [AvaloniaFact]
        public void Ctrl_up_on_filter_chain_list_moves_selected_filter()
        {
            var (window, viewModel, list, _) = FilterChainTestUi.ShowSeededList(selectIndex: 1);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            FilterChainTestUi.PressKeyOnControl(list, Key.Up, KeyModifiers.Control);
            Dispatcher.UIThread.RunJobs();

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
            var (window, viewModel, _, view) = FilterChainTestUi.ShowSeededList(selectIndex: 0);

            var moveDownButton = view.FindControl<Button>("MoveFilterChainDownButton");
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
        public async Task Clear_button_removes_all_filters()
        {
            var (window, viewModel, list, view) = FilterChainTestUi.ShowSeededList(selectIndex: 0);
            // Accept via hook (not Suppress): ConfigStore suppressions race with parallel suites.
            viewModel.UiHooks = new FilterChainUiHooks { ConfirmClearAsync = () => Task.FromResult(true) };

            var clearButton = view.FindControl<Button>("ClearFilterChainButton");
            Assert.NotNull(clearButton);
            Assert.NotNull(viewModel.ClearCommand);
            Assert.True(viewModel.ClearCommand.CanExecute(null));
            await viewModel.ClearCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewModel.Steps);
            Assert.Empty(viewModel.SelectedSteps);
            Assert.Equal(0, list.ItemCount);

            window.Close();
        }
    }
}
