using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.Filters.Misc;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Unit tests for <see cref="FixLeadingZerosFilterEditorViewModel"/>.
    /// </summary>
    public sealed class FixLeadingZerosFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies editor syncs palette defaults and live-replaces options.
        /// </summary>
        [Fact]
        public void Options_update_step_filter()
        {
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", new FixLeadingZerosFilter());
            var editor = new FixLeadingZerosFilterEditorViewModel(step);

            Assert.Equal(2, editor.Width);
            Assert.False(editor.RemoveExtraZeros);
            Assert.Equal(1, editor.MaxCount);
            Assert.True(editor.WholeWordOnly);

            editor.Width = 4;
            editor.RemoveExtraZeros = true;
            editor.MaxCount = 0;
            editor.WholeWordOnly = false;

            var options = ((FixLeadingZerosFilter)step.Filter).Options;
            Assert.Equal(4, options.Width);
            Assert.True(options.RemoveExtraZeros);
            Assert.Equal(0, options.MaxCount);
            Assert.False(options.WholeWordOnly);
        }

        /// <summary>
        /// Verifies spinner values clamp into filter option ranges.
        /// </summary>
        [Fact]
        public void Width_and_MaxCount_clamp_to_option_ranges()
        {
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", new FixLeadingZerosFilter());
            var editor = new FixLeadingZerosFilterEditorViewModel(step) { Width = 0, MaxCount = -1 };
            var low = ((FixLeadingZerosFilter)step.Filter).Options;
            Assert.Equal(1, low.Width);
            Assert.Equal(0, low.MaxCount);

            editor.Width = 99;
            editor.MaxCount = 20_000;
            var high = ((FixLeadingZerosFilter)step.Filter).Options;
            Assert.Equal(30, high.Width);
            Assert.Equal(9999, high.MaxCount);
        }

        /// <summary>
        /// Verifies the editor loads options from an existing applied step.
        /// </summary>
        [Fact]
        public void Syncs_from_existing_filter_options()
        {
            var filter = new FixLeadingZerosFilter(
                new FilePrefixTarget(),
                new FixLeadingZerosOptions(Width: 5, RemoveExtraZeros: true, MaxCount: 3, WholeWordOnly: false)
            );
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", filter);
            var editor = new FixLeadingZerosFilterEditorViewModel(step);

            Assert.Equal(5, editor.Width);
            Assert.True(editor.RemoveExtraZeros);
            Assert.Equal(3, editor.MaxCount);
            Assert.False(editor.WholeWordOnly);
            Assert.Same(filter, step.Filter);
        }
    }
}
