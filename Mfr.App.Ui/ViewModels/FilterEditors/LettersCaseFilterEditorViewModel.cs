using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Filter Configuration editor for <see cref="LettersCaseFilter"/>.
    /// </summary>
    internal sealed partial class LettersCaseFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isSyncing;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        /// <param name="filter">Current <see cref="LettersCaseFilter"/> instance.</param>
        public LettersCaseFilterEditorViewModel(AppliedFilterStepViewModel step, LettersCaseFilter filter)
            : base(step)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _SyncFromFilter(filter);
        }

        /// <summary>
        /// Gets the casing-mode choices.
        /// </summary>
        public IReadOnlyList<LettersCaseModeOption> ModeOptions => LettersCaseModeOption.All;

        /// <summary>
        /// Gets or sets the selected casing mode.
        /// </summary>
        [ObservableProperty]
        private LettersCaseModeOption? _selectedMode;

        /// <summary>
        /// Gets whether skip-words editing is available for the current mode.
        /// </summary>
        [ObservableProperty]
        private bool _hasSkipWords;

        /// <summary>
        /// Gets or sets comma-separated skip words for title case.
        /// </summary>
        [ObservableProperty]
        private string _skipWordsText = string.Empty;

        partial void OnSelectedModeChanged(LettersCaseModeOption? value)
        {
            if (_isSyncing)
            {
                return;
            }

            HasSkipWords = value?.Mode == LettersCaseMode.TitleCase;
            _ApplyOptions();
        }

        partial void OnSkipWordsTextChanged(string value)
        {
            if (_isSyncing)
            {
                return;
            }

            _ApplyOptions();
        }

        private void _SyncFromFilter(LettersCaseFilter filter)
        {
            _isSyncing = true;
            try
            {
                SelectedMode = LettersCaseModeOption.FromMode(filter.Options.Mode);
                SkipWordsText = string.Join(", ", filter.Options.SkipWords);
                HasSkipWords = filter.Options.Mode == LettersCaseMode.TitleCase;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isSyncing || SelectedMode is null || Step.Filter is not LettersCaseFilter filter)
            {
                return;
            }

            var skipWords = _ParseSkipWords(SkipWordsText);
            var options = filter.Options with { Mode = SelectedMode.Mode, SkipWords = skipWords };

            if (filter.Options == options)
            {
                return;
            }

            Step.SetFilter(filter with { Options = options });
        }

        private static IReadOnlyList<string> _ParseSkipWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            return [.. text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }
    }
}
