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
        private bool _isLoading;

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
        /// Gets or sets the selected casing mode.
        /// </summary>
        [ObservableProperty]
        private LettersCaseMode _mode;

        /// <summary>
        /// Gets or sets whether Capitalize is selected.
        /// </summary>
        public bool IsModeCapitalize
        {
            get => Mode == LettersCaseMode.Capitalize;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.Capitalize;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether Sentence case is selected.
        /// </summary>
        public bool IsModeSentenceCase
        {
            get => Mode == LettersCaseMode.SentenceCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.SentenceCase;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether tOGGLE cASE is selected.
        /// </summary>
        public bool IsModeInvertCase
        {
            get => Mode == LettersCaseMode.InvertCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.InvertCase;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether UPPER CASE is selected.
        /// </summary>
        public bool IsModeUpperCase
        {
            get => Mode == LettersCaseMode.UpperCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.UpperCase;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether First letter up is selected.
        /// </summary>
        public bool IsModeFirstLetterUp
        {
            get => Mode == LettersCaseMode.FirstLetterUp;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.FirstLetterUp;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether wEiRd CaSe is selected.
        /// </summary>
        public bool IsModeWeirdCase
        {
            get => Mode == LettersCaseMode.WeirdCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.WeirdCase;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether lower case is selected.
        /// </summary>
        public bool IsModeLowerCase
        {
            get => Mode == LettersCaseMode.LowerCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.LowerCase;
                }
            }
        }

        /// <summary>
        /// Gets whether skip-words editing is available for the current mode.
        /// </summary>
        public bool HasCapitalizeSkipWords => Mode == LettersCaseMode.Capitalize;

        /// <summary>
        /// Gets or sets comma-separated skip words for capitalize mode.
        /// </summary>
        [ObservableProperty]
        private string _capitalizeSkipWordsText = string.Empty;

        partial void OnModeChanged(LettersCaseMode value)
        {
            OnPropertyChanged(nameof(IsModeCapitalize));
            OnPropertyChanged(nameof(IsModeSentenceCase));
            OnPropertyChanged(nameof(IsModeInvertCase));
            OnPropertyChanged(nameof(IsModeUpperCase));
            OnPropertyChanged(nameof(IsModeFirstLetterUp));
            OnPropertyChanged(nameof(IsModeWeirdCase));
            OnPropertyChanged(nameof(IsModeLowerCase));
            OnPropertyChanged(nameof(HasCapitalizeSkipWords));
            _ApplyOptions();
        }

        partial void OnCapitalizeSkipWordsTextChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter(LettersCaseFilter filter)
        {
            _isLoading = true;
            try
            {
                Mode = filter.Options.Mode;
                CapitalizeSkipWordsText = string.Join(", ", filter.Options.CapitalizeSkipWords);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not LettersCaseFilter filter)
            {
                return;
            }

            var capitalizeSkipWords = _ParseCapitalizeSkipWords(CapitalizeSkipWordsText);
            var options = filter.Options with { Mode = Mode, CapitalizeSkipWords = capitalizeSkipWords };
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
