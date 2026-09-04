using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Space
{
    /// <summary>
    /// Shared Filter Configuration editor for <see cref="SpaceAfterFilter"/> and <see cref="SpaceAroundFilter"/>.
    /// </summary>
    internal sealed partial class SpaceTriggerFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public SpaceTriggerFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            (CharsPrompt, NeighborCheckLabel, NeighborCheckToolTip) = _ResolveLabels(step.Filter);
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the prompt beside the trigger-characters box.
        /// </summary>
        public string CharsPrompt { get; }

        /// <summary>
        /// Gets the neighbor-condition checkbox label.
        /// </summary>
        public string NeighborCheckLabel { get; }

        /// <summary>
        /// Gets the neighbor-condition checkbox tooltip.
        /// </summary>
        public string NeighborCheckToolTip { get; }

        /// <summary>
        /// Gets or sets the characters that trigger separator insertion.
        /// </summary>
        [ObservableProperty]
        private string _chars = string.Empty;

        /// <summary>
        /// Gets or sets whether insertion requires a letter/digit neighbor on the relevant side(s).
        /// </summary>
        [ObservableProperty]
        private bool _onlyWhenNeighborLetterOrDigit;

        partial void OnCharsChanged(string value) => _ApplyOptions();

        partial void OnOnlyWhenNeighborLetterOrDigitChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            LoadWithoutApplying(() =>
            {
                if (Step.Filter is SpaceAfterFilter after)
                {
                    Chars = after.Options.AfterChars;
                    OnlyWhenNeighborLetterOrDigit = after.Options.OnlyWhenNextIsLetterOrDigit;
                    return;
                }

                if (Step.Filter is SpaceAroundFilter around)
                {
                    Chars = around.Options.AroundChars;
                    OnlyWhenNeighborLetterOrDigit = around.Options.OnlyWhenNeighboringAreLettersOrDigits;
                }
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading)
            {
                return;
            }

            if (Step.Filter is SpaceAfterFilter after)
            {
                ApplyIfChanged(
                    after,
                    after with
                    {
                        Options = new SpaceAfterOptions(
                            AfterChars: Chars ?? string.Empty,
                            OnlyWhenNextIsLetterOrDigit: OnlyWhenNeighborLetterOrDigit
                        ),
                    }
                );
                return;
            }

            if (Step.Filter is SpaceAroundFilter around)
            {
                ApplyIfChanged(
                    around,
                    around with
                    {
                        Options = new SpaceAroundOptions(
                            AroundChars: Chars ?? string.Empty,
                            OnlyWhenNeighboringAreLettersOrDigits: OnlyWhenNeighborLetterOrDigit
                        ),
                    }
                );
            }
        }

        private static (string CharsPrompt, string NeighborCheckLabel, string NeighborCheckToolTip) _ResolveLabels(
            BaseFilter filter
        )
        {
            return filter switch
            {
                SpaceAfterFilter => (
                    "Ensure a space after each of the following characters:",
                    "Only when next character is a letter or a digit.",
                    "When checked, insert only if the character immediately after the trigger is a Unicode letter or digit."
                ),
                SpaceAroundFilter => (
                    "Ensure a space before and after each of the following characters:",
                    "Only when neighboring characters are letters or digits.",
                    "When checked, insert on each side only if that neighbor is a Unicode letter or digit."
                ),
                _ => throw new InvalidOperationException(
                    $"Space trigger editor does not support {filter.GetType().Name}."
                ),
            };
        }
    }
}
