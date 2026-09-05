using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Unit tests for <see cref="CounterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class CounterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Counter option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Counter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Counter", new CounterFilter());

            var editor = new CounterFilterEditorViewModel(step);

            Assert.Equal(1, editor.Start);

            Assert.Equal(1, editor.Increment);

            Assert.Equal(CounterLeadingZerosMode.None, editor.LeadingZerosMode);

            Assert.Equal(2, editor.CustomLength);

            Assert.False(editor.HasCustomLength);

            Assert.Equal(CounterPosition.Prepend, editor.Position);

            Assert.Equal(" - ", editor.Separator);

            Assert.True(editor.ResetPerFolder);

            Assert.True(editor.HasSeparatorOptions);

            editor.Start = 10;

            editor.Increment = 5;

            editor.LeadingZerosMode = CounterLeadingZerosMode.Custom;

            editor.CustomLength = 3;

            editor.Position = CounterPosition.Replace;

            editor.Separator = "_";

            editor.ResetPerFolder = false;

            Assert.True(editor.HasCustomLength);

            Assert.False(editor.HasSeparatorOptions);

            var options = ((CounterFilter)step.Filter).Options;

            Assert.Equal(10, options.Start);

            Assert.Equal(5, options.Step);

            Assert.Equal(CounterLeadingZerosMode.Custom, options.LeadingZerosMode);

            Assert.Equal(3, options.CustomLength);

            Assert.Equal(CounterPosition.Replace, options.Position);

            Assert.Equal("_", options.Separator);

            Assert.False(options.ResetPerFolder);

            editor.Position = CounterPosition.Append;

            editor.LeadingZerosMode = CounterLeadingZerosMode.Automatic;

            options = ((CounterFilter)step.Filter).Options;

            Assert.Equal(CounterPosition.Append, options.Position);

            Assert.Equal(CounterLeadingZerosMode.Automatic, options.LeadingZerosMode);

            Assert.True(editor.HasSeparatorOptions);

            Assert.False(editor.HasCustomLength);
        }
    }
}
