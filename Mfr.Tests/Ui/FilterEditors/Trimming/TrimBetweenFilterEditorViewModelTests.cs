using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Unit tests for <see cref="TrimBetweenFilterEditorViewModel"/>.
    /// </summary>
    public sealed class TrimBetweenFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Trim Between position/anchor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Trim_between_positions_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);

            Assert.Equal(2, editor.StartValue);
            Assert.Equal(Side.Left, editor.StartAnchor);
            Assert.Equal(4, editor.EndValue);
            Assert.Equal(Side.Left, editor.EndAnchor);

            editor.StartValue = 13;
            editor.EndValue = 5;
            editor.EndAnchor = Side.Right;

            var options = ((TrimBetweenFilter)step.Filter).Options;
            Assert.Equal(new Position(13, Side.Left), options.Start);
            Assert.Equal(new Position(5, Side.Right), options.End);
        }
    }
}
