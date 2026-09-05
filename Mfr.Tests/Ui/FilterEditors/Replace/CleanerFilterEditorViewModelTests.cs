using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.Filters.Replace;

namespace Mfr.Tests.Ui.FilterEditors.Replace
{
    /// <summary>
    /// Unit tests for <see cref="CleanerFilterEditorViewModel"/>.
    /// </summary>
    public sealed class CleanerFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Cleaner option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Cleaner_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Cleaner", new CleanerFilter());
            var editor = new CleanerFilterEditorViewModel(step);

            Assert.True(editor.RemoveIllegalChars);
            Assert.Equal(@"!""#$%&'()*+,/:;<=>?@[]\^`{}|~", editor.CustomCharsToRemove);
            Assert.False(editor.ReplaceWith);
            Assert.Equal(string.Empty, editor.Replacement);

            editor.RemoveIllegalChars = false;
            editor.CustomCharsToRemove = "@#";
            editor.Replacement = "_";
            editor.ReplaceWith = true;

            var options = ((CleanerFilter)step.Filter).Options;
            Assert.False(options.RemoveIllegalChars);
            Assert.Equal("@#", options.CustomCharsToRemove);
            Assert.Equal("_", options.Replacement);

            editor.ReplaceWith = false;

            options = ((CleanerFilter)step.Filter).Options;
            Assert.Equal(string.Empty, options.Replacement);
            Assert.Equal("_", editor.Replacement);
        }
    }
}
