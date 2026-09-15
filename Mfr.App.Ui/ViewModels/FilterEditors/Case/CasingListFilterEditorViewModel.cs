using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.FilterChain;
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
        public CasingListFilterEditorViewModel(FilterChainStepViewModel step)
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

        partial void OnWordsTextChanged(string value)
        {
            if (IsLoading)
            {
                return;
            }

            ScheduleLiveListTextApply(_ApplyOptions, value.Length);
        }

        partial void OnUppercaseSentenceInitialChanged(bool value)
        {
            FlushPendingLiveListTextApply();
            _ApplyOptions();
        }

        private void _SyncFromFilter()
        {
            if (Step.Filter is not CasingListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                WordsText = CasingListParser.FormatEditorText(filter.Options.Words);
                UppercaseSentenceInitial = filter.Options.UppercaseSentenceInitial;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not CasingListFilter)
            {
                return;
            }

            var text = WordsText;
            var uppercaseSentenceInitial = UppercaseSentenceInitial;
            ParseListTextThenApply(
                text,
                getCurrentText: () => WordsText,
                parse: CasingListParser.ParseEditorText,
                applyParsed: words =>
                {
                    if (Step.Filter is not CasingListFilter filter)
                    {
                        return;
                    }

                    var options = new CasingListOptions(
                        Words: words,
                        UppercaseSentenceInitial: uppercaseSentenceInitial
                    );
                    ApplyIfChanged(filter, filter with { Options = options });
                }
            );
        }
    }
}
