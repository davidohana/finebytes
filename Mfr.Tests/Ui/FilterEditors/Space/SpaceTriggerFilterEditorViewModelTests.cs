using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.FilterEditors.Space
{
    /// <summary>
    /// Unit tests for <see cref="SpaceTriggerFilterEditorViewModel"/>.
    /// </summary>
    public sealed class SpaceTriggerFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Space After chars/neighbor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Space_after_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Space After", new SpaceAfterFilter());
            var editor = new SpaceTriggerFilterEditorViewModel(step);

            Assert.Equal(",;!", editor.Chars);
            Assert.True(editor.OnlyWhenNeighborLetterOrDigit);
            Assert.Contains("after", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = ".,";
            editor.OnlyWhenNeighborLetterOrDigit = false;

            var options = ((SpaceAfterFilter)step.Filter).Options;
            Assert.Equal(".,", options.AfterChars);
            Assert.False(options.OnlyWhenNextIsLetterOrDigit);
        }

        /// <summary>
        /// Verifies Space Around chars/neighbor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Space_around_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Space Around", new SpaceAroundFilter());
            var editor = new SpaceTriggerFilterEditorViewModel(step);

            Assert.Equal("-", editor.Chars);
            Assert.True(editor.OnlyWhenNeighborLetterOrDigit);
            Assert.Contains("before and after", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = "+=";
            editor.OnlyWhenNeighborLetterOrDigit = false;

            var options = ((SpaceAroundFilter)step.Filter).Options;
            Assert.Equal("+=", options.AroundChars);
            Assert.False(options.OnlyWhenNeighboringAreLettersOrDigits);
        }
    }
}
