using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Case
{
    /// <summary>
    /// Shared Filter Configuration editor for <see cref="CapitalizeAfterFilter"/> and
    /// <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    internal sealed partial class CharacterListFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CharacterListFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            (CharsPrompt, CharsToolTip) = _ResolveLabels(step.Filter);
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the prompt beside the character-list box.
        /// </summary>
        public string CharsPrompt { get; }

        /// <summary>
        /// Gets the character-list tooltip.
        /// </summary>
        public string CharsToolTip { get; }

        /// <summary>
        /// Gets or sets the character list edited by this filter.
        /// </summary>
        [ObservableProperty]
        private string _chars = string.Empty;

        partial void OnCharsChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            LoadWithoutApplying(() =>
            {
                Chars = _ReadChars(Step.Filter);
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading)
            {
                return;
            }

            var chars = Chars ?? string.Empty;
            switch (Step.Filter)
            {
                case CapitalizeAfterFilter after:
                    ApplyIfChanged(
                        after,
                        after with
                        {
                            Options = new CapitalizeAfterOptions(CapitalizeAfterChars: chars),
                        }
                    );
                    return;
                case SentenceEndCharactersFilter sentenceEnd:
                    ApplyIfChanged(
                        sentenceEnd,
                        sentenceEnd with
                        {
                            Options = new SentenceEndCharactersOptions(Characters: chars),
                        }
                    );
                    return;
                default:
                    throw _UnsupportedFilter(Step.Filter);
            }
        }

        /// <summary>
        /// Reads the character-list option from a supported filter.
        /// </summary>
        private static string _ReadChars(BaseFilter filter)
        {
            return filter switch
            {
                CapitalizeAfterFilter after => after.Options.CapitalizeAfterChars,
                SentenceEndCharactersFilter sentenceEnd => sentenceEnd.Options.Characters,
                _ => throw _UnsupportedFilter(filter),
            };
        }

        private static (string CharsPrompt, string CharsToolTip) _ResolveLabels(BaseFilter filter)
        {
            return filter switch
            {
                CapitalizeAfterFilter => (
                    "Capitalize letters which succeed the following characters:",
                    "Each character in this list is a trigger.\nThe next character in the name is uppercased.\nEmpty list leaves the name unchanged."
                ),
                SentenceEndCharactersFilter => (
                    "The following characters indicate that a sentence had ended:",
                    "Characters that end a sentence for Letters Case (sentence mode) and Casing List (sentence initials).\nThis filter does not change the name text."
                ),
                _ => throw _UnsupportedFilter(filter),
            };
        }

        private static InvalidOperationException _UnsupportedFilter(BaseFilter filter)
        {
            return new InvalidOperationException($"Character list editor does not support {filter.GetType().Name}.");
        }
    }
}
