using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Case
{
    /// <summary>
    /// Filter Configuration editor for <see cref="CasingListFilter"/>.
    /// </summary>
    internal sealed partial class CasingListFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CasingListFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the space-separated editor text for the casing list.
        /// </summary>
        [ObservableProperty]
        private string _wordsText = string.Empty;

        /// <summary>
        /// Gets or sets whether sentence-initial letters are uppercased after list application.
        /// </summary>
        [ObservableProperty]
        private bool _uppercaseSentenceInitial = true;

        partial void OnWordsTextChanged(string value) => _ApplyOptions();

        partial void OnUppercaseSentenceInitialChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not CasingListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                WordsText = string.Join(' ', filter.Options.Words);
                UppercaseSentenceInitial = filter.Options.UppercaseSentenceInitial;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not CasingListFilter filter)
            {
                return;
            }

            var words = _ParseWords(WordsText);
            if (
                words.SequenceEqual(filter.Options.Words)
                && UppercaseSentenceInitial == filter.Options.UppercaseSentenceInitial
            )
            {
                return;
            }

            var options = new CasingListOptions(Words: words, UppercaseSentenceInitial: UppercaseSentenceInitial);
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Splits space-separated editor text into words.
        /// </summary>
        private static string[] _ParseWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
