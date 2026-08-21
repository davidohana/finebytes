using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels.FilterPalette
{
    /// <summary>
    /// Available Filters pane: group toolbar, quick search, and catalog list.
    /// </summary>
    public sealed partial class FilterPaletteViewModel : ViewModelBase
    {
        private static readonly StringComparer s_DisplayNameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Initializes the palette from <see cref="FilterCatalog"/>.
        /// </summary>
        public FilterPaletteViewModel()
        {
            Groups =
            [
                new FilterPaletteGroupViewModel(null, "All", "FilterGroupAllGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Case, "Case", "FilterGroupCaseGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Space, "Space", "FilterGroupSpaceGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Trimming, "Trimming", "FilterGroupTrimmingGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Replace, "Replace", "FilterGroupReplaceGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Formatting, "Formatting", "FilterGroupFormattingGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Audio, "Audio", "FilterGroupAudioGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Attributes, "Attributes", "FilterGroupAttributesGeometry"),
                new FilterPaletteGroupViewModel(FilterGroup.Misc, "Misc", "FilterGroupMiscGeometry"),
            ];

            VisibleFilters = [];
            Groups[0].IsSelected = true;
            _RefreshVisibleFilters();
        }

        /// <summary>
        /// Gets the exclusive group toolbar buttons (All first, then the eight groups).
        /// </summary>
        public IReadOnlyList<FilterPaletteGroupViewModel> Groups { get; }

        /// <summary>
        /// Gets the filters visible for the current group and search text.
        /// </summary>
        public ObservableCollection<FilterCatalogEntry> VisibleFilters { get; }

        /// <summary>
        /// Gets or sets the selected group; <see langword="null"/> means All.
        /// </summary>
        [ObservableProperty]
        private FilterGroup? _selectedGroup;

        /// <summary>
        /// Gets or sets the quick-filter text (case-insensitive substring).
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Gets or sets the currently selected catalog row.
        /// </summary>
        [ObservableProperty]
        private FilterCatalogEntry? _selectedFilter;

        /// <summary>
        /// Selects an exclusive group button (All or a concrete group).
        /// </summary>
        /// <param name="groupButton">The toolbar button to activate.</param>
        [RelayCommand]
        public void SelectGroup(FilterPaletteGroupViewModel? groupButton)
        {
            if (groupButton is null)
            {
                return;
            }

            if (SelectedGroup == groupButton.Group)
            {
                _SyncGroupSelectionFlags();
                return;
            }

            SelectedGroup = groupButton.Group;
        }

        partial void OnSelectedGroupChanged(FilterGroup? value)
        {
            _SyncGroupSelectionFlags();
            _RefreshVisibleFilters();
        }

        partial void OnSearchTextChanged(string value)
        {
            _RefreshVisibleFilters();
        }

        private void _SyncGroupSelectionFlags()
        {
            foreach (var button in Groups)
            {
                button.IsSelected = button.Group == SelectedGroup;
            }
        }

        private void _RefreshVisibleFilters()
        {
            var previous = SelectedFilter;
            var query = SearchText.Trim();
            var hasQuery = query.Length > 0;

            var filtered = FilterCatalog
                .Entries.Where(entry => SelectedGroup is null || entry.Group == SelectedGroup)
                .Where(entry => !hasQuery || _MatchesSearch(entry, query))
                .OrderBy(entry => entry.DisplayName, s_DisplayNameComparer)
                .ToList();

            VisibleFilters.Clear();
            foreach (var entry in filtered)
            {
                VisibleFilters.Add(entry);
            }

            if (previous is not null && filtered.Contains(previous))
            {
                SelectedFilter = previous;
                return;
            }

            SelectedFilter = filtered.Count > 0 ? filtered[0] : null;
        }

        private static bool _MatchesSearch(FilterCatalogEntry entry, string query)
        {
            return entry.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || entry.Type.Contains(query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
