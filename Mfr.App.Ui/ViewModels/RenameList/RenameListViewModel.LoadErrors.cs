using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Load Errors command and focused-cell state for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the user requests the Show Load Errors dialog.
        /// </summary>
        public event EventHandler<RenameListLoadErrorsDialogContent>? LoadErrorsDialogRequested;

        /// <summary>
        /// Gets whether Show Load Errors should appear on the row context menu.
        /// </summary>
        public bool CanShowLoadErrors => _CanShowLoadErrors();

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
        [RelayCommand(CanExecute = nameof(_CanShowLoadErrors))]
        public void ShowLoadErrors()
        {
            if (_selectedEntries.Count != 1)
            {
                return;
            }

            var entry = _selectedEntries[0];
            var errors = RenameListFieldCatalog.ListLoadErrors(entry.EngineItem);
            if (errors.Count == 0)
            {
                return;
            }

            LoadErrorsDialogRequested?.Invoke(this, new RenameListLoadErrorsDialogContent(entry.FullPath, errors));
        }

        private bool _CanShowLoadErrors()
        {
            if (IsAdding || _selectedEntries.Count != 1)
            {
                return false;
            }

            return RenameListFieldCatalog.HasAnyLoadError(_selectedEntries[0].EngineItem);
        }

        private void _NotifyShowLoadErrorsChanged()
        {
            OnPropertyChanged(nameof(CanShowLoadErrors));
            ShowLoadErrorsCommand.NotifyCanExecuteChanged();
        }
    }
}
