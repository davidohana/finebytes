using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Clear-command confirmation gates for Applied Filters.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class AppliedFiltersClearConfirmTests
    {
        public AppliedFiltersClearConfirmTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies replace-on-load confirm is required only for More with a non-empty stack.
        /// </summary>
        [Fact]
        public void NeedsConfirmReplaceOnLoad_requires_More_and_nonempty_stack()
        {
            var viewModel = new AppliedFiltersViewModel();
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad());

            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            Assert.True(viewModel.NeedsConfirmReplaceOnLoad());

            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Normal;
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad());
        }

        /// <summary>
        /// Verifies More prompts before clearing a non-empty stack and clears when accepted.
        /// </summary>
        [Fact]
        public async Task Clear_More_accept_clears_stack()
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var asked = 0;
            viewModel.UiHooks = new AppliedFiltersUiHooks
            {
                ConfirmClearAsync = () =>
                {
                    asked++;
                    return Task.FromResult(true);
                },
            };

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Equal(1, asked);
            Assert.Empty(viewModel.Steps);
        }

        /// <summary>
        /// Verifies More declines leave the Applied Filters stack unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_More_decline_keeps_stack()
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.UiHooks = new AppliedFiltersUiHooks { ConfirmClearAsync = () => Task.FromResult(false) };

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Single(viewModel.Steps);
        }

        /// <summary>
        /// Verifies Normal clears without calling the confirm hook.
        /// </summary>
        [Fact]
        public async Task Clear_Normal_skips_confirm()
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Normal;
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var asked = 0;
            viewModel.UiHooks = new AppliedFiltersUiHooks
            {
                ConfirmClearAsync = () =>
                {
                    asked++;
                    return Task.FromResult(false);
                },
            };

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Equal(0, asked);
            Assert.Empty(viewModel.Steps);
        }

        /// <summary>
        /// Verifies More with a missing confirm hook aborts and leaves the stack unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_More_null_hook_aborts()
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Single(viewModel.Steps);
        }
    }
}
