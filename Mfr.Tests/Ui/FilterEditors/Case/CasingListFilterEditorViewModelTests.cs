using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.Filters.Case;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Unit tests for <see cref="CasingListFilterEditorViewModel"/>.
    /// </summary>
    public sealed class CasingListFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Casing List option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Casing_list_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Casing List", new CasingListFilter());
            var editor = new CasingListFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.WordsText);
            Assert.True(editor.UppercaseSentenceInitial);

            editor.WordsText = "and or RMX";
            editor.UppercaseSentenceInitial = false;

            var options = ((CasingListFilter)step.Filter).Options;
            Assert.Equal(["and", "or", "RMX"], options.Words);
            Assert.False(options.UppercaseSentenceInitial);
        }
    }
}
