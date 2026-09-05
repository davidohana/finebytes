using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Replace;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Replace
{
    /// <summary>
    /// Filter Configuration editor for <see cref="ReplaceListFilter"/>.
    /// </summary>
    internal sealed partial class ReplaceListFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public ReplaceListFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            Match = new ReplacerMatchOptionsEditor(defaultWholeWord: true);
            Match.Bind(_OnMatchChanged);
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets shared mode and match-flag fields.
        /// </summary>
        public ReplacerMatchOptionsEditor Match { get; }

        /// <summary>
        /// Gets or sets the line-separated editor text for search/replace pairs.
        /// </summary>
        [ObservableProperty]
        private string _entriesText = string.Empty;

        /// <summary>
        /// Gets a mode-specific example watermark for an empty entries box.
        /// </summary>
        public string EntriesWatermark =>
            Match.Mode switch
            {
                ReplacerMode.Literal => ". => _\nfeat. => feature.\nLive",
                ReplacerMode.Wildcard => "DSC*.JPG => photo.jpg\ntrack?.mp3 => track0.mp3\n*.tmp",
                ReplacerMode.Regex => "[0-9]+ => N\n\\. => _\n\\s+ => _",
                _ => throw new ArgumentOutOfRangeException(nameof(Match.Mode), Match.Mode, null),
            };

        partial void OnEntriesTextChanged(string value) => _ApplyOptions(parseEntries: true);

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ReplaceListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                EntriesText = ReplaceListParser.FormatEditorText(filter.Options.Entries);
                Match.Load(filter.Options.Match);
            });
        }

        /// <summary>
        /// Notifies mode-dependent UI and writes options when match fields change.
        /// </summary>
        private void _OnMatchChanged()
        {
            OnPropertyChanged(nameof(EntriesWatermark));
            _ApplyOptions(parseEntries: false);
        }

        /// <summary>
        /// Writes current editor state onto the applied step filter.
        /// </summary>
        /// <param name="parseEntries">
        /// When true, rebuilds entries from <see cref="EntriesText"/>; when false, keeps the step's
        /// structured entries (avoids re-parsing lossy text for searches that contain <c>=&gt;</c>).
        /// </param>
        private void _ApplyOptions(bool parseEntries)
        {
            if (IsLoading || Step.Filter is not ReplaceListFilter filter)
            {
                return;
            }

            var entries = parseEntries ? ReplaceListParser.ParseEditorText(EntriesText) : filter.Options.Entries;
            var options = new ReplaceListOptions(Entries: entries, Match: Match.ToOptions());
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
