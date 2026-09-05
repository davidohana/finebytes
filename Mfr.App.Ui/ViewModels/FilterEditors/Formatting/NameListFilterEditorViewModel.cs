using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Formatting
{
    /// <summary>
    /// Filter Configuration editor for <see cref="NameListFilter"/>.
    /// </summary>
    internal sealed partial class NameListFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public NameListFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the line-separated editor text for name-list entries.
        /// </summary>
        [ObservableProperty]
        private string _entriesText = string.Empty;

        /// <summary>
        /// Gets or sets the format string prepended to each list entry.
        /// </summary>
        [ObservableProperty]
        private string _prefix = string.Empty;

        /// <summary>
        /// Gets or sets the format string appended after each list entry.
        /// </summary>
        [ObservableProperty]
        private string _suffix = string.Empty;

        partial void OnEntriesTextChanged(string value) => _ApplyOptions();

        partial void OnPrefixChanged(string value) => _ApplyOptions();

        partial void OnSuffixChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not NameListFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                EntriesText = NameListParser.FormatEditorText(filter.Options.Entries);
                Prefix = filter.Options.Prefix;
                Suffix = filter.Options.Suffix;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not NameListFilter filter)
            {
                return;
            }

            var options = new NameListOptions(
                Entries: NameListParser.ParseEditorText(EntriesText),
                Prefix: Prefix,
                Suffix: Suffix
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
