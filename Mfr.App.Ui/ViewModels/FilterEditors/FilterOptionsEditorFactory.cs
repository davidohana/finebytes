using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Attributes;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Builds type-specific option editors for the Filter Configuration pane.
    /// </summary>
    internal static class FilterOptionsEditorFactory
    {
        /// <summary>
        /// Creates an options editor for <paramref name="step"/>, or <see langword="null"/> when unsupported.
        /// </summary>
        /// <param name="step">Selected applied-filter row.</param>
        /// <returns>Editor view model, or <see langword="null"/> for optionless / not-yet-implemented types.</returns>
        internal static FilterOptionsEditorViewModel? Create(AppliedFilterStepViewModel step)
        {
            ArgumentNullException.ThrowIfNull(step);

            return step.Filter switch
            {
                SpaceCharacterFilter => new SpaceCharacterFilterEditorViewModel(step),
                SpaceAfterFilter or SpaceAroundFilter => new SpaceTriggerFilterEditorViewModel(step),
                LettersCaseFilter => new LettersCaseFilterEditorViewModel(step),
                CapitalizeAfterFilter or SentenceEndCharactersFilter => new CharacterListFilterEditorViewModel(step),
                CasingListFilter => new CasingListFilterEditorViewModel(step),
                ICountOptionsFilter => new CountFilterEditorViewModel(step),
                ShrinkDuplicateCharactersFilter => new ShrinkDuplicateCharactersFilterEditorViewModel(step),
                TrimBetweenFilter => new TrimBetweenFilterEditorViewModel(step),
                FixLeadingZerosFilter => new FixLeadingZerosFilterEditorViewModel(step),
                StripParenthesesFilter => new StripParenthesesFilterEditorViewModel(step),
                MoverFilter => new MoverFilterEditorViewModel(step),
                CleanerFilter => new CleanerFilterEditorViewModel(step),
                ReplacerFilter => new ReplacerFilterEditorViewModel(step),
                ReplaceListFilter => new ReplaceListFilterEditorViewModel(step),
                CounterFilter => new CounterFilterEditorViewModel(step),
                InserterFilter => new InserterFilterEditorViewModel(step),
                NameListFilter => new NameListFilterEditorViewModel(step),
                TokenMoverFilter => new TokenMoverFilterEditorViewModel(step),
                DateSetterFilter or TimeSetterFilter => new DateTimeSetterFilterEditorViewModel(step),
                _ => null,
            };
        }
    }
}
