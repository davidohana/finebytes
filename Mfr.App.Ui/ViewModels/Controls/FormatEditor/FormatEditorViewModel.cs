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
            _RefreshVisibleEntries();
        }

        /// <summary>
        /// Gets or sets the insert-picker search filter.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Gets the catalog rows visible for the current search.
        /// </summary>
        public IReadOnlyList<FormatTokenCatalogEntry> VisibleEntries { get; private set; } = [];

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

        partial void OnSearchTextChanged(string value) => _RefreshVisibleEntries();

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
        /// Rebuilds <see cref="VisibleEntries"/> from the catalog for the current search text.
        /// </summary>
        private void _RefreshVisibleEntries()
        {
            var query = SearchText.Trim();
            IEnumerable<FormatTokenCatalogEntry> entries = FormatTokenCatalog.Entries;
            if (query.Length > 0)
            {
                entries = entries.Where(e =>
                    e.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || e.CanonicalName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || e.ShortDescription.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || e.GroupPath.Contains(query, StringComparison.OrdinalIgnoreCase)
                );
            }

            VisibleEntries = [.. entries];
            OnPropertyChanged(nameof(VisibleEntries));
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
