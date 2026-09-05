using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/> host selection/title behavior.
    /// </summary>
    public sealed class FilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies an empty Applied selection clears the configuration title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_no_selection_clears_title()
        {
            var editor = new FilterEditorViewModel();

            editor.SyncSelection([]);

            Assert.False(editor.HasSelectedStep);

            Assert.Equal(string.Empty, editor.TitleText);
        }

        /// <summary>
        /// Verifies the first selected step sets the Applied Filter title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_one_step_sets_title()
        {
            var editor = new FilterEditorViewModel();

            var step = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());

            editor.SyncSelection([step]);

            Assert.True(editor.HasSelectedStep);

            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies multi-select uses the first selected row for the title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_multi_select_uses_first_row()
        {
            var editor = new FilterEditorViewModel();

            var first = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());

            var second = new AppliedFilterStepViewModel("Letters Case", new LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);

            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }
    }
}
