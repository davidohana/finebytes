using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Unit tests for <see cref="TokenMoverFilterEditorViewModel"/>.
    /// </summary>
    public sealed class TokenMoverFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Token Mover option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Token Mover", new TokenMoverFilter());
            var editor = new TokenMoverFilterEditorViewModel(step);

            Assert.Equal("-", editor.Delimiter);
            Assert.Equal(1, editor.TokenNumber);
            Assert.Equal(1, editor.MoveBy);

            editor.Delimiter = ",";
            editor.TokenNumber = 2;
            editor.MoveBy = -1;

            var options = ((TokenMoverFilter)step.Filter).Options;
            Assert.Equal(",", options.Delimiter);
            Assert.Equal(2, options.TokenNumber);
            Assert.Equal(-1, options.MoveBy);
        }

        /// <summary>
        /// Verifies spinner values outside the option bounds are clamped on apply.
        /// </summary>
        [Fact]
        public void Options_clamp_spinner_bounds()
        {
            var step = new AppliedFilterStepViewModel("Token Mover", new TokenMoverFilter());
            var editor = new TokenMoverFilterEditorViewModel(step)
            {
                TokenNumber = 0,
                MoveBy = -1000
            };

            var options = ((TokenMoverFilter)step.Filter).Options;
            Assert.Equal(1, options.TokenNumber);
            Assert.Equal(-999, options.MoveBy);

            editor.TokenNumber = 1001;
            editor.MoveBy = 1000;

            options = ((TokenMoverFilter)step.Filter).Options;
            Assert.Equal(1000, options.TokenNumber);
            Assert.Equal(999, options.MoveBy);
        }

        /// <summary>
        /// Verifies a null delimiter is stored as empty rather than null.
        /// </summary>
        [Fact]
        public void Options_null_delimiter_becomes_empty()
        {
            var step = new AppliedFilterStepViewModel("Token Mover", new TokenMoverFilter());
            _ = new TokenMoverFilterEditorViewModel(step) { Delimiter = null! };

            Assert.Equal(string.Empty, ((TokenMoverFilter)step.Filter).Options.Delimiter);
        }
    }
}
