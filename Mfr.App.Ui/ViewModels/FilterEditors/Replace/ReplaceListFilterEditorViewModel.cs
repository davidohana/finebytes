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
                ReplacerMode.Wildcard => "DSC*.JPG => photo.jpg\ntrack?.mp3 => track0.mp3\n*.tmp",
                ReplacerMode.Regex => "[0-9]+ => N\n\\. => _\n\\s+ => _",
                _ => ". => _\nfeat. => feature.\nLive",
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

        partial void OnEntriesTextChanged(string value) => _ApplyOptions();

        partial void OnModeChanged(ReplacerMode value) => _ApplyOptions();

        partial void OnCaseSensitiveChanged(bool value) => _ApplyOptions();

        partial void OnReplaceAllChanged(bool value) => _ApplyOptions();

        partial void OnWholeWordChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ReplaceListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                EntriesText = _FormatEntries(filter.Options.Entries);
                Mode = filter.Options.Mode;
                CaseSensitive = filter.Options.CaseSensitive;
                ReplaceAll = filter.Options.ReplaceAll;
                WholeWord = filter.Options.WholeWord;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not ReplaceListFilter filter)
            {
                return;
            }

            var options = new ReplaceListOptions(
                Entries: _ParseEntries(EntriesText),
                Mode: Mode,
                CaseSensitive: CaseSensitive,
                ReplaceAll: ReplaceAll,
                WholeWord: WholeWord
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Formats stored entries as line-separated <c>search =&gt; replacement</c> pairs for the editor.
        /// </summary>
        private static string _FormatEntries(IReadOnlyList<ReplaceListEntry> entries)
        {
            if (entries.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(
                '\n',
                entries.Select(e =>
                    e.Replacement.Length == 0
                        ? e.Search
                        : $"{e.Search} {ReplaceListEntry.EditorSeparator} {e.Replacement}"
                )
            );
        }

        /// <summary>
        /// Parses line-separated pairs using <see cref="ReplaceListEntry.EditorSeparator"/> (first occurrence);
        /// a line without the separator is search-only (strip).
        /// </summary>
        private static ReplaceListEntry[] _ParseEntries(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.None);
            var entries = new List<ReplaceListEntry>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var sepIndex = line.IndexOf(ReplaceListEntry.EditorSeparator, StringComparison.Ordinal);
                if (sepIndex < 0)
                {
                    var stripSearch = line.Trim();
                    if (stripSearch.Length == 0)
                    {
                        continue;
                    }

                    entries.Add(new ReplaceListEntry(stripSearch, string.Empty));
                    continue;
                }

                var search = line[..sepIndex].Trim();
                if (search.Length == 0)
                {
                    continue;
                }

                var replacement = line[(sepIndex + ReplaceListEntry.EditorSeparator.Length)..].Trim();
                entries.Add(new ReplaceListEntry(search, replacement));
            }

            return [.. entries];
        }
    }
}
