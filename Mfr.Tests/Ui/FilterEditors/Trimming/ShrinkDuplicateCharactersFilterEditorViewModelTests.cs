using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Unit tests for <see cref="ShrinkDuplicateCharactersFilterEditorViewModel"/>.
    /// </summary>
    public sealed class ShrinkDuplicateCharactersFilterEditorViewModelTests
    {
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
