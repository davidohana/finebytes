using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// Validation and Edit chrome state for the shared <see cref="Views.FormatEditor.FormatEditor"/> control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound format string lives on the control's <c>Text</c> property; this VM owns the last validation
    /// result for the error row and Edit-under-caret. Insert catalog state lives on
    /// <see cref="FormatTokenInsertPickerViewModel"/>.
    /// </para>
    /// </remarks>
    public sealed partial class FormatEditorViewModel : ViewModelBase
    {
        /// <summary>Max characters shown on the inline error link before truncation.</summary>
        private const int MaxInlineErrorLength = 120;

        private readonly Action _jumpToError;
        private readonly Action _editUnderCaret;

        /// <summary>
        /// Initializes error and Edit state for a FormatEditor instance.
        /// </summary>
        /// <param name="jumpToError">Selects the last validation error span.</param>
        /// <param name="editUnderCaret">Opens the token editor (or warning) for the caret token.</param>
        public FormatEditorViewModel(Action jumpToError, Action editUnderCaret)
        {
            _jumpToError = jumpToError;
            _editUnderCaret = editUnderCaret;
        }

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

        /// <summary>
        /// Gets or sets how templates are validated (always vs only when likely tokens are present).
        /// </summary>
        public FormatStringValidationMode ValidationMode { get; set; } = FormatStringValidationMode.Always;

        /// <summary>
        /// Re-validates <paramref name="template"/> and updates the error row.
        /// </summary>
        /// <param name="template">Current format string.</param>
        public void Validate(string template)
        {
            var result = FormatStringSyntax.TryValidate(template ?? string.Empty, ValidationMode);
            LastParseResult = result;
            HasError = !result.Success;
            FullErrorMessage = result.ErrorMessage;
            ErrorMessage = _TruncateError(result.ErrorMessage);
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
