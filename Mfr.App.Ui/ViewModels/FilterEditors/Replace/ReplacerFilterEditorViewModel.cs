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
            Match = new ReplacerMatchOptionsEditor(defaultWholeWord: false);
            Match.Bind(_OnMatchChanged);
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets shared mode and match-flag fields.
        /// </summary>
        public ReplacerMatchOptionsEditor Match { get; }

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
        /// Gets a mode-specific tooltip for the Find field.
        /// </summary>
        public string FindToolTip =>
            Match.Mode switch
            {
                ReplacerMode.Literal => "Exact text to find in the target.\nSpecial characters are matched literally.",
                ReplacerMode.Wildcard =>
                    "Pattern to find in the target.\n* matches any characters; ? matches one character.",
                ReplacerMode.Regex => "Regular expression to find in the target.\nUses .NET regex syntax.",
                _ => throw new ArgumentOutOfRangeException(nameof(Match.Mode), Match.Mode, null),
            };

        /// <summary>
        /// Gets a mode-specific tooltip for the Replace field.
        /// </summary>
        public string ReplacementToolTip =>
            Match.Mode switch
            {
                ReplacerMode.Literal or ReplacerMode.Wildcard =>
                    "Replacement for each match.\nLeave empty to strip matches.",
                ReplacerMode.Regex =>
                    "Replacement for each match.\nLeave empty to strip matches.\n$0 / $1… refer to captured groups.",
                _ => throw new ArgumentOutOfRangeException(nameof(Match.Mode), Match.Mode, null),
            };

        /// <summary>
        /// Gets a mode-specific example watermark for an empty Find box.
        /// </summary>
        public string FindWatermark =>
            Match.Mode switch
            {
                ReplacerMode.Literal => "feat.",
                ReplacerMode.Wildcard => "DSC*.JPG",
                ReplacerMode.Regex => @"\((.+)\)",
                _ => throw new ArgumentOutOfRangeException(nameof(Match.Mode), Match.Mode, null),
            };

        /// <summary>
        /// Gets a mode-specific example watermark for an empty Replace box.
        /// </summary>
        public string ReplacementWatermark =>
            Match.Mode switch
            {
                ReplacerMode.Literal => "feature.",
                ReplacerMode.Wildcard => "photo.jpg",
                ReplacerMode.Regex => "$1",
                _ => throw new ArgumentOutOfRangeException(nameof(Match.Mode), Match.Mode, null),
            };

        partial void OnFindChanged(string value) => _ApplyOptions();

        partial void OnReplacementChanged(string value) => _ApplyOptions();

        /// <summary>
        /// Loads option fields from the applied <see cref="ReplacerFilter"/> without writing back.
        /// </summary>
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
                Match.Load(filter.Options.Match);
            });
        }

        /// <summary>
        /// Notifies mode-dependent UI and writes options when match fields change.
        /// </summary>
        private void _OnMatchChanged()
        {
            OnPropertyChanged(nameof(FindToolTip));
            OnPropertyChanged(nameof(ReplacementToolTip));
            OnPropertyChanged(nameof(FindWatermark));
            OnPropertyChanged(nameof(ReplacementWatermark));
            _ApplyOptions();
        }

        /// <summary>
        /// Writes the current editor fields onto the step filter when they differ.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not ReplacerFilter filter)
            {
                return;
            }

            var options = new ReplacerOptions(
                Find: Find ?? string.Empty,
                Replacement: Replacement ?? string.Empty,
                Match: Match.ToOptions()
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
