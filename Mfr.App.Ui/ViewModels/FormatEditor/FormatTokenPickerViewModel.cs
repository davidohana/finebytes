using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// Searchable format-token catalog for the Insert flyout or
    /// <see cref="Views.FormatEditor.FormatTokenToolsHost"/>.
    /// </summary>
    public sealed partial class FormatTokenPickerViewModel : ViewModelBase
    {
        private readonly Action<string> _insertText;

        /// <summary>
        /// Initializes catalog state for a format-token picker.
        /// </summary>
        /// <param name="insertText">Inserts catalog text at the caret / selection.</param>
        public FormatTokenPickerViewModel(Action<string> insertText)
        {
            _insertText = insertText;
            _RefreshVisibleItems();
        }

        /// <summary>
        /// Gets or sets the token-picker search filter.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Gets token-picker roots: nested <see cref="FormatTokenCatalogEntry.GroupPath"/> folders when
        /// search is empty; flat catalog leaves when filtering.
        /// </summary>
        public IReadOnlyList<FormatTokenPickerNode> VisibleItems { get; private set; } = [];

        /// <summary>
        /// Gets whether the picker is nested by group (search empty / whitespace) rather than a flat filter result.
        /// </summary>
        public bool IsGrouped => SearchText.Trim().Length == 0;

        partial void OnSearchTextChanged(string value) => _RefreshVisibleItems();

        /// <summary>
        /// Inserts a catalog row's default text at the caret.
        /// </summary>
        /// <param name="entry">Catalog entry chosen from the picker.</param>
        [RelayCommand]
        public void InsertEntry(FormatTokenCatalogEntry? entry)
        {
            if (entry is null)
            {
                return;
            }

            _insertText(entry.InsertText);
            SearchText = string.Empty;
        }

        /// <summary>
        /// Rebuilds <see cref="VisibleItems"/> from the catalog for the current search text.
        /// </summary>
        private void _RefreshVisibleItems()
        {
            var query = SearchText.Trim();
            VisibleItems =
                query.Length == 0 ? _BuildGroupedItems(FormatTokenCatalog.Entries) : _BuildFlatFilteredItems(query);

            OnPropertyChanged(nameof(VisibleItems));
            OnPropertyChanged(nameof(IsGrouped));
        }

        /// <summary>
        /// Flat catalog leaves matching <paramref name="query"/> (group subtitle shown).
        /// </summary>
        private static List<FormatTokenPickerNode> _BuildFlatFilteredItems(string query)
        {
            return
            [
                .. FormatTokenCatalog
                    .Entries.Where(e =>
                        e.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || e.CanonicalName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || e.ShortDescription.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || e.GroupPath.Contains(query, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(e => FormatTokenPickerNode.Leaf(e, showGroupSubtitle: true)),
            ];
        }

        /// <summary>
        /// Nests catalog rows under <see cref="FormatTokenCatalogEntry.GroupPath"/> segments (<c>\</c>-separated).
        /// </summary>
        private static List<FormatTokenPickerNode> _BuildGroupedItems(IReadOnlyList<FormatTokenCatalogEntry> entries)
        {
            var root = new GroupBuilder(title: string.Empty);
            foreach (var entry in entries)
            {
                var segments = entry.GroupPath.Split(
                    '\\',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                var group = root;
                foreach (var segment in segments)
                {
                    group = group.Child(segment);
                }

                group.Entries.Add(entry);
            }

            return root.ToNodes();
        }

        /// <summary>
        /// Mutable folder used while folding catalog rows into a <see cref="FormatTokenPickerNode"/> tree.
        /// </summary>
        private sealed class GroupBuilder(string title)
        {
            private readonly Dictionary<string, GroupBuilder> _nameToChild = new(StringComparer.OrdinalIgnoreCase);
            private readonly List<GroupBuilder> _childOrder = [];

            /// <summary>Gets the folder segment name.</summary>
            public string Title { get; } = title;

            /// <summary>Gets leaves under this folder (catalog order).</summary>
            public List<FormatTokenCatalogEntry> Entries { get; } = [];

            /// <summary>
            /// Returns an existing or new child folder named <paramref name="segment"/>.
            /// </summary>
            /// <param name="segment">Single path segment (case-insensitive match to existing children).</param>
            /// <returns>Child folder builder.</returns>
            public GroupBuilder Child(string segment)
            {
                if (_nameToChild.TryGetValue(segment, out var existing))
                {
                    return existing;
                }

                var child = new GroupBuilder(segment);
                _nameToChild[segment] = child;
                _childOrder.Add(child);
                return child;
            }

            /// <summary>
            /// Materializes child groups then leaves (group-first, matching browse menus).
            /// </summary>
            /// <returns>Nodes for this folder's children and catalog leaves.</returns>
            public List<FormatTokenPickerNode> ToNodes()
            {
                var nodes = new List<FormatTokenPickerNode>(_childOrder.Count + Entries.Count);
                foreach (var child in _childOrder)
                {
                    nodes.Add(FormatTokenPickerNode.Group(child.Title, child.ToNodes()));
                }

                foreach (var entry in Entries)
                {
                    nodes.Add(FormatTokenPickerNode.Leaf(entry, showGroupSubtitle: false));
                }

                return nodes;
            }
        }
    }
}
