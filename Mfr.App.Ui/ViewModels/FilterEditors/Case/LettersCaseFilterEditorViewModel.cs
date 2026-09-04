using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Case
{
    /// <summary>
    /// Filter Configuration editor for <see cref="LettersCaseFilter"/>.
    /// </summary>
    internal sealed partial class LettersCaseFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public LettersCaseFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the selected casing mode.
        /// </summary>
        [ObservableProperty]
        private LettersCaseMode _mode;

        /// <summary>
        /// Gets whether skip-words editing is available for the current mode.
        /// </summary>
        public bool HasCapitalizeSkipWords => Mode == LettersCaseMode.Capitalize;

        /// <summary>
        /// Gets whether weird-case settings are available for the current mode.
        /// </summary>
        public bool HasWeirdCaseOptions => Mode == LettersCaseMode.WeirdCase;

        /// <summary>
        /// Gets or sets comma-separated skip words for capitalize mode.
        /// </summary>
        [ObservableProperty]
        private string _capitalizeSkipWordsText = string.Empty;

        /// <summary>
        /// Gets or sets the uppercase chance for weird case (0–100).
        /// </summary>
        [ObservableProperty]
        private decimal _weirdUppercaseChancePercent = 50;

        /// <summary>
        /// Gets or sets whether weird-case decisions depend only on character position.
        /// </summary>
        [ObservableProperty]
        private bool _weirdFixedPlaces;

        partial void OnModeChanged(LettersCaseMode value)
        {
            OnPropertyChanged(nameof(HasCapitalizeSkipWords));
            OnPropertyChanged(nameof(HasWeirdCaseOptions));
            _ApplyOptions();
        }

        partial void OnCapitalizeSkipWordsTextChanged(string value) => _ApplyOptions();

        partial void OnWeirdUppercaseChancePercentChanged(decimal value) => _ApplyOptions();

        partial void OnWeirdFixedPlacesChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not LettersCaseFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                Mode = filter.Options.Mode;
                CapitalizeSkipWordsText = string.Join(", ", filter.Options.CapitalizeSkipWords);
                WeirdUppercaseChancePercent = filter.Options.WeirdUppercaseChancePercent;
                WeirdFixedPlaces = filter.Options.WeirdFixedPlaces;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not LettersCaseFilter filter)
            {
                return;
            }

            var capitalizeSkipWords = _ParseCapitalizeSkipWords(CapitalizeSkipWordsText);
            var options = filter.Options with
            {
                Mode = Mode,
                CapitalizeSkipWords = capitalizeSkipWords,
                WeirdUppercaseChancePercent = ClampToInt(WeirdUppercaseChancePercent, 0, 100),
                WeirdFixedPlaces = WeirdFixedPlaces,
            };
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static IReadOnlyList<string> _ParseCapitalizeSkipWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            return [.. text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }
    }
}
