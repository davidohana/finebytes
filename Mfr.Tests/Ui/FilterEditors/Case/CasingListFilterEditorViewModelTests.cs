using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.Filters.Case;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Unit tests for <see cref="CasingListFilterEditorViewModel"/>.
    /// </summary>
    public sealed class CasingListFilterEditorViewModelTests
    {
        public CasingListFilterEditorViewModelTests()
        {
            FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds = 0;
        }

        /// <summary>
        /// Verifies Casing List option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Casing_list_options_update_step_options()
        {
            var step = new FilterChainStepViewModel("Casing List", new CasingListFilter());
            var editor = new CasingListFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.WordsText);
            Assert.True(editor.UppercaseSentenceInitial);

            editor.WordsText = "and or RMX";
            editor.UppercaseSentenceInitial = false;

            var options = ((CasingListFilter)step.Filter).Options;
            Assert.Equal(["and", "or", "RMX"], options.Words);
            Assert.False(options.UppercaseSentenceInitial);
        }

        /// <summary>
        /// Verifies Load defaults replaces existing custom Words with the factory list.
        /// </summary>
        [Fact]
        public void LoadDefaults_ReplacesCustomWordsText()
        {
            var step = new FilterChainStepViewModel("Casing List", new CasingListFilter());
            var editor = new CasingListFilterEditorViewModel(step) { WordsText = "custom foo BAR" };
            Assert.Equal(["custom", "foo", "BAR"], ((CasingListFilter)step.Filter).Options.Words);

            editor.LoadDefaultsCommand.Execute(null);

            Assert.Equal(CasingListParser.FormatEditorText(CasingListOptions.DefaultWords), editor.WordsText);
            Assert.Equal(CasingListOptions.DefaultWords, ((CasingListFilter)step.Filter).Options.Words);
        }

        /// <summary>
        /// Verifies Load defaults commits the factory list without waiting for list-text debounce.
        /// </summary>
        [Fact]
        public void LoadDefaults_AppliesImmediatelyWithLiveDebounceEnabled()
        {
            var priorDebounce = FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds;
            try
            {
                FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds = 150;

                var step = new FilterChainStepViewModel("Casing List", new CasingListFilter());
                var editor = new CasingListFilterEditorViewModel(step) { WordsText = "custom" };
                editor.FlushPendingLiveListTextApply();
                Assert.Equal(["custom"], ((CasingListFilter)step.Filter).Options.Words);

                editor.LoadDefaultsCommand.Execute(null);

                Assert.Equal(CasingListOptions.DefaultWords, ((CasingListFilter)step.Filter).Options.Words);
            }
            finally
            {
                FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds = priorDebounce;
            }
        }
    }
}
