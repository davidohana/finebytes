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
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the line-separated editor text for search/replace pairs.
        /// </summary>
        [ObservableProperty]
        private string _entriesText = string.Empty;

        /// <summary>
        /// Gets or sets the pattern interpretation mode for all pairs.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EntriesWatermark))]
        private ReplacerMode _mode = ReplacerMode.Literal;

        /// <summary>
        /// Gets a mode-specific example watermark for an empty entries box.
        /// </summary>
        public string EntriesWatermark =>
            Mode switch
            {
                ReplacerMode.Literal => ". => _\nfeat. => feature.\nLive",
                ReplacerMode.Wildcard => "DSC*.JPG => photo.jpg\ntrack?.mp3 => track0.mp3\n*.tmp",
                ReplacerMode.Regex => "[0-9]+ => N\n\\. => _\n\\s+ => _",
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null),
            };

        /// <summary>
        /// Gets or sets whether matching is case-sensitive.
        /// </summary>
        [ObservableProperty]
        private bool _caseSensitive;

        /// <summary>
        /// Gets or sets whether all matches are replaced per pair.
        /// </summary>
        [ObservableProperty]
        private bool _replaceAll = true;

        /// <summary>
        /// Gets or sets whether matching is constrained to whole words.
        /// </summary>
        [ObservableProperty]
        private bool _wholeWord = true;

        partial void OnEntriesTextChanged(string value) => _ApplyOptions(parseEntries: true);

        partial void OnModeChanged(ReplacerMode value) => _ApplyOptions(parseEntries: false);

        partial void OnCaseSensitiveChanged(bool value) => _ApplyOptions(parseEntries: false);

        partial void OnReplaceAllChanged(bool value) => _ApplyOptions(parseEntries: false);

        partial void OnWholeWordChanged(bool value) => _ApplyOptions(parseEntries: false);

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ReplaceListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                EntriesText = ReplaceListParser.FormatEditorText(filter.Options.Entries);
                Mode = filter.Options.Mode;
                CaseSensitive = filter.Options.CaseSensitive;
                ReplaceAll = filter.Options.ReplaceAll;
                WholeWord = filter.Options.WholeWord;
            });
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
            var options = new ReplaceListOptions(
                Entries: entries,
                Mode: Mode,
                CaseSensitive: CaseSensitive,
                ReplaceAll: ReplaceAll,
                WholeWord: WholeWord
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
