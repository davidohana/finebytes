using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Rename Error command for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Gets whether Show Rename Error should appear on the row context menu.
        /// </summary>
        public bool CanShowCommitError => _CanShowCommitError();

        /// <summary>
        /// Shows the last commit failure for the selected row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowCommitError))]
        public void ShowCommitError()
        {
            if (!_CanShowCommitError())
            {
                return;
            }

            var entry = _selectedEntries[0];
            var commitError = entry.EngineItem.CommitError;
            if (commitError is null)
            {
                return;
            }

            RowErrorDialogRequested?.Invoke(
                this,
                RenameListCommitErrorDisplay.Create(
                    entry.FullPath,
                    commitError.Message,
                    RenameListRowErrorDisplay.FormatExceptionDetails(commitError.Cause)
                )
            );
        }

        private bool _CanShowCommitError()
        {
            if (IsBusy || _selectedEntries.Count != 1)
            {
                return false;
            }

            return _selectedEntries[0].HasCommitError;
        }

        private void _NotifyShowCommitErrorChanged()
        {
            OnPropertyChanged(nameof(CanShowCommitError));
            OnPropertyChanged(nameof(CanShowRowErrorMenu));
            ShowCommitErrorCommand.NotifyCanExecuteChanged();
        }
    }
}
