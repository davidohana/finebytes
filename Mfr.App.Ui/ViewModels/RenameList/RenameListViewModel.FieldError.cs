using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Field Error command and focused-cell state for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the user requests the Show Field Error dialog.
        /// </summary>
        public event EventHandler<RenameListFieldErrorDialogContent>? FieldErrorDialogRequested;

        /// <summary>
        /// Gets whether Show Field Error should appear on the row context menu.
        /// </summary>
        public bool CanShowFieldError => _CanShowFieldError();

        /// <summary>
        /// Gets the focused Rename List column for status hints and Show Field Error.
        /// </summary>
        public RenameListFieldKey? FocusedFieldKey { get; private set; }

        /// <summary>
        /// Updates the focused column and refreshes Show Field Error availability.
        /// </summary>
        /// <param name="fieldKey">Focused grid column key, or <see langword="null"/> when unset.</param>
        public void SetFocusedFieldKey(RenameListFieldKey? fieldKey)
        {
            if (FocusedFieldKey == fieldKey)
            {
                return;
            }

            FocusedFieldKey = fieldKey;
            OnPropertyChanged(nameof(FocusedFieldKey));
            _NotifyShowFieldErrorChanged();
        }

        /// <summary>
        /// Shows the stored metadata load exception for the focused original cell.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowFieldError))]
        public void ShowFieldError()
        {
            var entry = _GetFocusedSelectedEntry();
            if (entry is null || FocusedFieldKey is not { } fieldKey)
            {
                return;
            }

            var error = entry.TryGetFieldLoadError(fieldKey);
            var field = RenameListFieldCatalog.GetField(fieldKey);
            var userExplanation = RenameListFieldCatalog.DescribeFieldLoadError(entry.EngineItem, fieldKey);
            FieldErrorDialogRequested?.Invoke(
                this,
                new RenameListFieldErrorDialogContent(
                    field.DisplayName,
                    userExplanation,
                    error?.Message ?? string.Empty
                )
            );
        }

        private bool _CanShowFieldError()
        {
            if (IsAdding || _selectedEntries.Count != 1 || FocusedFieldKey is not { } fieldKey)
            {
                return false;
            }

            if (fieldKey.IsPreview)
            {
                return false;
            }

            return _selectedEntries[0].IsFieldLoadError(fieldKey);
        }

        private void _NotifyShowFieldErrorChanged()
        {
            OnPropertyChanged(nameof(CanShowFieldError));
            ShowFieldErrorCommand.NotifyCanExecuteChanged();
        }
    }
}
