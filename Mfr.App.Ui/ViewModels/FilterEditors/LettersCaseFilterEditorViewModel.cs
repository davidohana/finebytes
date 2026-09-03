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
        /// Gets or sets whether Capitalize (title case) is selected.
        /// </summary>
        public bool IsModeTitleCase
        {
            get => Mode == LettersCaseMode.TitleCase;
            set
            {
                if (value)
                {
                    Mode = LettersCaseMode.TitleCase;
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
        public bool HasSkipWords => Mode == LettersCaseMode.TitleCase;

        /// <summary>
        /// Gets or sets comma-separated skip words for title case.
        /// </summary>
        [ObservableProperty]
        private string _skipWordsText = string.Empty;

        partial void OnModeChanged(LettersCaseMode value)
        {
            OnPropertyChanged(nameof(IsModeTitleCase));
            OnPropertyChanged(nameof(IsModeSentenceCase));
            OnPropertyChanged(nameof(IsModeInvertCase));
            OnPropertyChanged(nameof(IsModeUpperCase));
            OnPropertyChanged(nameof(IsModeFirstLetterUp));
            OnPropertyChanged(nameof(IsModeWeirdCase));
            OnPropertyChanged(nameof(IsModeLowerCase));
            OnPropertyChanged(nameof(HasSkipWords));
            _ApplyOptions();
        }

        partial void OnSkipWordsTextChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter(LettersCaseFilter filter)
        {
            _isLoading = true;
            try
            {
                Mode = filter.Options.Mode;
                SkipWordsText = string.Join(", ", filter.Options.SkipWords);
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

            var skipWords = _ParseSkipWords(SkipWordsText);
            var options = filter.Options with { Mode = Mode, SkipWords = skipWords };
            ApplyIfChanged(filter, filter with { Options = options });
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
