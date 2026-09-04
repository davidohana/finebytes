using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/>.
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
            var second = new AppliedFilterStepViewModel("Letters Case", new Filters.Case.LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies Shrink Duplicate Characters character edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Shrink_duplicate_character_text_updates_step_options()
        {
            var step = new AppliedFilterStepViewModel(
                "Shrink Duplicate Characters",
                new ShrinkDuplicateCharactersFilter()
            );
            var editor = new ShrinkDuplicateCharactersFilterEditorViewModel(step);

            Assert.Equal("-", editor.CharacterText);
            Assert.Equal('-', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);

            editor.CharacterText = ">";
            Assert.Equal('>', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);

            editor.CharacterText = string.Empty;
            Assert.Equal('\0', ((ShrinkDuplicateCharactersFilter)step.Filter).Options.Character);
        }

        /// <summary>
        /// Verifies an empty/null character on the filter loads as an empty editor field.
        /// </summary>
        [Fact]
        public void Shrink_duplicate_null_character_loads_as_empty_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Shrink Duplicate Characters",
                new ShrinkDuplicateCharactersFilter(
                    new FilePrefixTarget(),
                    new ShrinkDuplicateCharactersOptions(Character: '\0')
                )
            );
            var editor = new ShrinkDuplicateCharactersFilterEditorViewModel(step);

            Assert.Equal(string.Empty, editor.CharacterText);
        }
    }
}
