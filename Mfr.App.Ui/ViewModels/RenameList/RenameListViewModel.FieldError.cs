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
        /// Gets the focused Rename List column for status hints.
        /// </summary>
        public RenameListFieldKey? FocusedFieldKey { get; private set; }

        /// <summary>
        /// Updates the focused column used for status-bar cell hints.
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
        }

        /// <summary>
        /// Shows stored metadata load failures for the selected row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowFieldError))]
        public void ShowFieldError()
        {
            if (_selectedEntries.Count != 1)
            {
                return;
            }

            var entry = _selectedEntries[0];
            var errors = RenameListFieldCatalog.ListFieldLoadErrors(entry.EngineItem);
            if (errors.Count == 0)
            {
                return;
            }

            FieldErrorDialogRequested?.Invoke(
                this,
                new RenameListFieldErrorDialogContent(entry.FullPath, errors)
            );
        }

        private bool _CanShowFieldError()
        {
            if (IsAdding || _selectedEntries.Count != 1)
            {
                return false;
            }

            return RenameListFieldCatalog.HasAnyFieldLoadError(_selectedEntries[0].EngineItem);
        }

        private void _NotifyShowFieldErrorChanged()
        {
            OnPropertyChanged(nameof(CanShowFieldError));
            ShowFieldErrorCommand.NotifyCanExecuteChanged();
        }
    }
}
