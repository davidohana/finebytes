using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Unit tests for <see cref="FormatterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class FormatterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Formatter template edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Formatter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Formatter", new FormatterFilter());
            var editor = new FormatterFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.Template);

            editor.Template = "<file-name>_<counter:initial=1,step=1>";

            var options = ((FormatterFilter)step.Filter).Options;
            Assert.Equal("<file-name>_<counter:initial=1,step=1>", options.Template);
        }
    }
}
