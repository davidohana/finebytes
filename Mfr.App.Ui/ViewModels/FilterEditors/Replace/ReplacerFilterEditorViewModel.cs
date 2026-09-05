using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Replace;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Replace
{
    /// <summary>
    /// Filter Configuration editor for <see cref="ReplacerFilter"/>.
    /// </summary>
    internal sealed partial class ReplacerFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public ReplacerFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the search pattern.
        /// </summary>
        [ObservableProperty]
        private string _find = string.Empty;

        /// <summary>
        /// Gets or sets the replacement text.
        /// </summary>
        [ObservableProperty]
        private string _replacement = string.Empty;

        /// <summary>
        /// Gets or sets the pattern interpretation mode.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FindToolTip))]
        [NotifyPropertyChangedFor(nameof(ReplacementToolTip))]
        private ReplacerMode _mode = ReplacerMode.Literal;

        /// <summary>
        /// Gets a mode-specific tooltip for the Find field.
        /// </summary>
        public string FindToolTip =>
            Mode switch
            {
                ReplacerMode.Literal => "Exact text to find in the target.\nSpecial characters are matched literally.",
                ReplacerMode.Wildcard =>
                    "Pattern to find in the target.\n* matches any characters; ? matches one character.",
                ReplacerMode.Regex => "Regular expression to find in the target.\nUses .NET regex syntax.",
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null),
            };

        /// <summary>
        /// Gets a mode-specific tooltip for the Replace field.
        /// </summary>
        public string ReplacementToolTip =>
            Mode switch
            {
                ReplacerMode.Literal or ReplacerMode.Wildcard =>
                    "Replacement for each match.\nLeave empty to strip matches.",
                ReplacerMode.Regex =>
                    "Replacement for each match.\nLeave empty to strip matches.\n$0 / $1… refer to captured groups.",
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null),
            };

        /// <summary>
        /// Gets or sets whether matching is case-sensitive.
        /// </summary>
        [ObservableProperty]
        private bool _caseSensitive;

        /// <summary>
        /// Gets or sets whether all matches are replaced.
        /// </summary>
        [ObservableProperty]
        private bool _replaceAll = true;

        /// <summary>
        /// Gets or sets whether matching is constrained to whole words.
        /// </summary>
        [ObservableProperty]
        private bool _wholeWord;

        partial void OnFindChanged(string value) => _ApplyOptions();

        partial void OnReplacementChanged(string value) => _ApplyOptions();

        partial void OnModeChanged(ReplacerMode value) => _ApplyOptions();

        partial void OnCaseSensitiveChanged(bool value) => _ApplyOptions();

        partial void OnReplaceAllChanged(bool value) => _ApplyOptions();

        partial void OnWholeWordChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ReplacerFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                Find = filter.Options.Find;
                Replacement = filter.Options.Replacement;
                Mode = filter.Options.Mode;
                CaseSensitive = filter.Options.CaseSensitive;
                ReplaceAll = filter.Options.ReplaceAll;
                WholeWord = filter.Options.WholeWord;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not ReplacerFilter filter)
            {
                return;
            }

            var options = new ReplacerOptions(
                Find: Find ?? string.Empty,
                Replacement: Replacement ?? string.Empty,
                Mode: Mode,
                CaseSensitive: CaseSensitive,
                ReplaceAll: ReplaceAll,
                WholeWord: WholeWord
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
