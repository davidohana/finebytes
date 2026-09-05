using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.Filters.Case;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Unit tests for <see cref="CharacterListFilterEditorViewModel"/>.
    /// </summary>
    public sealed class CharacterListFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Capitalize After trigger-char edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Capitalize_after_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Capitalize After", new CapitalizeAfterFilter());
            var editor = new CharacterListFilterEditorViewModel(step);

            Assert.Equal(",!()[]{};-", editor.Chars);
            Assert.Contains("succeed", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = "._";

            var options = ((CapitalizeAfterFilter)step.Filter).Options;
            Assert.Equal("._", options.CapitalizeAfterChars);
        }

        /// <summary>
        /// Verifies Sentence End Characters list edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Sentence_end_characters_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Sentence End Characters", new SentenceEndCharactersFilter());
            var editor = new CharacterListFilterEditorViewModel(step);

            Assert.Equal("-.!", editor.Chars);
            Assert.Contains("sentence had ended", editor.CharsPrompt, StringComparison.OrdinalIgnoreCase);

            editor.Chars = ":;";

            var options = ((SentenceEndCharactersFilter)step.Filter).Options;
            Assert.Equal(":;", options.Characters);
        }
    }
}
