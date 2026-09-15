using Mfr.App.Ui.ViewModels.FilterChain;

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
        /// Verifies replace-on-load confirm is required when not suppressed and the stack is non-empty.
        /// </summary>
        [Fact]
        public void NeedsConfirmReplaceOnLoad_requires_unsuppressed_and_nonempty_stack()
        {
            var viewModel = new FilterChainViewModel();
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad());

            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            Assert.True(viewModel.NeedsConfirmReplaceOnLoad());

            ConfirmationPolicy.Suppress(ConfirmationKind.ReplaceFilterChainOnLoad);
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad());
        }

        /// <summary>
        /// Verifies clear confirms by default and clears when accepted.
        /// </summary>
        [Fact]
        public async Task Clear_confirm_accept_clears_stack()
        {
            var viewModel = new FilterChainViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var asked = 0;
            viewModel.UiHooks = new FilterChainUiHooks
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
        /// Verifies decline leaves the Applied Filters stack unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_confirm_decline_keeps_stack()
        {
            var viewModel = new FilterChainViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.UiHooks = new FilterChainUiHooks { ConfirmClearAsync = () => Task.FromResult(false) };

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Single(viewModel.Steps);
        }

        /// <summary>
        /// Verifies a suppressed ClearFilterChain skips the confirm hook.
        /// </summary>
        [Fact]
        public async Task Clear_suppressed_skips_confirm()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.ClearFilterChain);
            var viewModel = new FilterChainViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var asked = 0;
            viewModel.UiHooks = new FilterChainUiHooks
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
        /// Verifies a missing confirm hook aborts and leaves the stack unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_null_hook_aborts()
        {
            var viewModel = new FilterChainViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            await viewModel.ClearCommand.ExecuteAsync(null);

            Assert.Single(viewModel.Steps);
        }
    }
}
