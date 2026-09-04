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
        private ReplacerMode _mode = ReplacerMode.Literal;

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
        /// Formats stored entries as line-separated whitespace pairs for the editor.
        /// </summary>
        private static string _FormatEntries(IReadOnlyList<ReplaceListEntry> entries)
        {
            if (entries.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(
                '\n',
                entries.Select(e => e.Replacement.Length == 0 ? e.Search : $"{e.Search} {e.Replacement}")
            );
        }

        /// <summary>
        /// Parses line-separated pairs: one token = strip; two tokens = search/replace; other lengths skipped.
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

                var parts = line.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                if (parts.Length == 1)
                {
                    entries.Add(new ReplaceListEntry(parts[0], string.Empty));
                    continue;
                }

                if (parts.Length != 2)
                {
                    continue;
                }

                entries.Add(new ReplaceListEntry(parts[0], parts[1]));
            }

            return [.. entries];
        }
    }
}
