using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor
{
    /// <summary>
    /// Picker / error state for the shared <see cref="Views.Controls.FormatEditor.FormatEditor"/> control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound format string lives on the control's <c>Text</c> property; this VM owns search, catalog
    /// filtering, and the last validation result for the error row.
    /// </para>
    /// </remarks>
    public sealed partial class FormatEditorViewModel : ViewModelBase
    {
        /// <summary>Max characters shown on the inline error link before truncation.</summary>
        private const int MaxInlineErrorLength = 120;

        private readonly Action<string> _insertText;
        private readonly Action _jumpToError;
        private readonly Action _editUnderCaret;

        /// <summary>
        /// Initializes picker and error state for a FormatEditor instance.
        /// </summary>
        /// <param name="insertText">Inserts catalog text at the caret / selection.</param>
        /// <param name="jumpToError">Selects the last validation error span.</param>
        /// <param name="editUnderCaret">Opens the token editor (or warning) for the caret token.</param>
        public FormatEditorViewModel(Action<string> insertText, Action jumpToError, Action editUnderCaret)
        {
            _insertText = insertText;
            _jumpToError = jumpToError;
            _editUnderCaret = editUnderCaret;
            _RefreshVisibleItems();
        }

        /// <summary>
        /// Gets or sets the insert-picker search filter.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Gets insert-picker roots: nested <see cref="FormatTokenCatalogEntry.GroupPath"/> folders when
        /// search is empty; flat catalog leaves when filtering.
        /// </summary>
        public IReadOnlyList<FormatInsertPickerNode> VisibleItems { get; private set; } = [];

        /// <summary>
        /// Gets whether the picker is nested by group (search empty / whitespace) rather than a flat filter result.
        /// </summary>
        public bool IsGrouped => SearchText.Trim().Length == 0;

        /// <summary>
        /// Gets whether the last validation failed.
        /// </summary>
        [ObservableProperty]
        private bool _hasError;

        /// <summary>
        /// Gets the truncated inline error message, or empty when valid.
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Gets the full error message for a details dialog.
        /// </summary>
        public string? FullErrorMessage { get; private set; }

        /// <summary>
        /// Gets the last successful-or-failed parse result (spans / error location).
        /// </summary>
        public FormatStringParseResult? LastParseResult { get; private set; }

        partial void OnSearchTextChanged(string value) => _RefreshVisibleItems();

        /// <summary>
        /// Re-validates <paramref name="template"/> and updates the error row.
        /// </summary>
        /// <param name="template">Current format string.</param>
        public void Validate(string template)
        {
            var result = FormatStringSyntax.TryValidate(template ?? string.Empty);
            LastParseResult = result;
            HasError = !result.Success;
            FullErrorMessage = result.ErrorMessage;
            ErrorMessage = _TruncateError(result.ErrorMessage);
        }

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
        /// Selects the failing span when validation failed.
        /// </summary>
        [RelayCommand]
        public void JumpToError()
        {
            _jumpToError();
        }

        /// <summary>
        /// Opens Edit for the token under the caret.
        /// </summary>
        [RelayCommand]
        public void Edit()
        {
            _editUnderCaret();
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
        private static List<FormatInsertPickerNode> _BuildFlatFilteredItems(string query)
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
                    .Select(e => FormatInsertPickerNode.Leaf(e, showGroupSubtitle: true)),
            ];
        }

        /// <summary>
        /// Nests catalog rows under <see cref="FormatTokenCatalogEntry.GroupPath"/> segments (<c>\</c>-separated).
        /// </summary>
        private static List<FormatInsertPickerNode> _BuildGroupedItems(IReadOnlyList<FormatTokenCatalogEntry> entries)
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
        /// Mutable folder used while folding catalog rows into a <see cref="FormatInsertPickerNode"/> tree.
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
            public List<FormatInsertPickerNode> ToNodes()
            {
                var nodes = new List<FormatInsertPickerNode>(_childOrder.Count + Entries.Count);
                foreach (var child in _childOrder)
                {
                    nodes.Add(FormatInsertPickerNode.Group(child.Title, child.ToNodes()));
                }

                foreach (var entry in Entries)
                {
                    nodes.Add(FormatInsertPickerNode.Leaf(entry, showGroupSubtitle: false));
                }

                return nodes;
            }
        }

        /// <summary>
        /// Truncates <paramref name="message"/> for the inline error link (full text stays in
        /// <see cref="FullErrorMessage"/>).
        /// </summary>
        private static string _TruncateError(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            if (message.Length <= MaxInlineErrorLength)
            {
                return message;
            }

            return message[..(MaxInlineErrorLength - 1)] + "…";
        }
    }
}
