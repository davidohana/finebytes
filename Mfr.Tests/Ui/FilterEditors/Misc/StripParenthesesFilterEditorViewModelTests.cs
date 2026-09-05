using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.Filters.Misc;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Unit tests for <see cref="StripParenthesesFilterEditorViewModel"/>.
    /// </summary>
    public sealed class StripParenthesesFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Strip Parentheses type/contents edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Strip_parentheses_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Strip Parentheses", new StripParenthesesFilter());
            var editor = new StripParenthesesFilterEditorViewModel(step);

            Assert.Equal(ParenthesisType.Round, editor.Type);
            Assert.True(editor.RemoveContents);

            editor.Type = ParenthesisType.Square;
            editor.RemoveContents = false;

            var options = ((StripParenthesesFilter)step.Filter).Options;
            Assert.Equal(ParenthesisType.Square, options.Type);
            Assert.False(options.RemoveContents);
        }
    }
}
