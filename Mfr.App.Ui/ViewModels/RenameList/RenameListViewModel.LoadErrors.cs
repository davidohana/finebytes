using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Load Errors command for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the user requests a row error dialog (load, preview, or later apply).
        /// </summary>
        public event EventHandler<RenameListRowErrorDialogContent>? RowErrorDialogRequested;

        /// <summary>
        /// Gets whether Show Load Errors should appear on the row context menu.
        /// </summary>
        public bool CanShowLoadErrors => _CanShowLoadErrors();

        /// <summary>
        /// Shows stored metadata load failures for the selected row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowLoadErrors))]
        public void ShowLoadErrors()
        {
            if (!_CanShowLoadErrors())
            {
                return;
            }

            var entry = _selectedEntries[0];
            var errors = RenameListFieldCatalog.ListLoadErrors(entry.EngineItem);
            RowErrorDialogRequested?.Invoke(this, RenameListLoadErrorDisplay.Create(entry.FullPath, errors));
        }

        private bool _CanShowLoadErrors()
        {
            if (IsBusy || _selectedEntries.Count != 1)
            {
                return false;
            }

            return RenameListFieldCatalog.HasAnyLoadError(_selectedEntries[0].EngineItem);
        }

        private void _NotifyShowLoadErrorsChanged()
        {
            OnPropertyChanged(nameof(CanShowLoadErrors));
            OnPropertyChanged(nameof(CanShowRowErrorMenu));
            ShowLoadErrorsCommand.NotifyCanExecuteChanged();
        }
    }
}
