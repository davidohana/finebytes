using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;
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
                SpaceCharacterFilter filter => new SpaceCharacterFilterEditorViewModel(step, filter),
                LettersCaseFilter filter => new LettersCaseFilterEditorViewModel(step, filter),
                TrimLeftFilter or TrimRightFilter or ExtractLeftFilter or ExtractRightFilter =>
                    new CountFilterEditorViewModel(step),
                _ => null,
            };
        }
    }
}
