using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for type-specific filter option editors.
    /// </summary>
    public sealed class FilterOptionsEditorViewModelTests
    {
        /// <summary>
        /// Verifies Space Character checkbox changes update <see cref="SpaceCharacterOptions"/> on the step.
        /// </summary>
        [Fact]
        public void Space_character_replace_underscores_updates_chain_options()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));
            var step = applied.Steps[0];
            var editor = new SpaceCharacterFilterEditorViewModel(step, (SpaceCharacterFilter)step.Filter)
            {
                ReplaceUnderscores = false
            };

            var filter = (SpaceCharacterFilter)applied.ToChain().Steps[0].Filter;
            Assert.False(filter.Options.ReplaceUnderscores);
            Assert.True(filter.Options.ReplaceSpaces);
        }

        /// <summary>
        /// Verifies Letters Case mode changes update <see cref="LettersCaseOptions"/> on the step.
        /// </summary>
        [Fact]
        public void Letters_case_mode_change_updates_chain_options()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var step = applied.Steps[0];
            var editor = new LettersCaseFilterEditorViewModel(step, (LettersCaseFilter)step.Filter)
            {
                SelectedMode = LettersCaseModeOption.FromMode(LettersCaseMode.UpperCase)
            };

            var filter = (LettersCaseFilter)applied.ToChain().Steps[0].Filter;
            Assert.Equal(LettersCaseMode.UpperCase, filter.Options.Mode);
        }

        /// <summary>
        /// Verifies title-case skip words round-trip through the editor.
        /// </summary>
        [Fact]
        public void Letters_case_skip_words_update_chain_options()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var step = applied.Steps[0];
            var editor = new LettersCaseFilterEditorViewModel(step, (LettersCaseFilter)step.Filter)
            {
                SkipWordsText = "a, the, for"
            };

            var filter = (LettersCaseFilter)applied.ToChain().Steps[0].Filter;
            Assert.Equal(["a", "the", "for"], filter.Options.SkipWords);
        }
    }
}
