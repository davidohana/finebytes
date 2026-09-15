using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Load / Preview / Rename Error commands for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the user requests a row error dialog (load, preview, or rename).
        /// </summary>
        public event EventHandler<RenameListRowErrorDialogContent>? RowErrorDialogRequested;

        /// <summary>
        /// Gets whether Show Load Errors should appear on the row context menu.
        /// </summary>
        public bool CanShowLoadErrors => _CanShowLoadErrors();

        /// <summary>
        /// Gets whether Show Preview Error should appear on the row context menu.
        /// </summary>
        public bool CanShowPreviewError => _CanShowPreviewError();

        /// <summary>
        /// Gets whether Show Rename Error should appear on the row context menu.
        /// </summary>
        public bool CanShowCommitError => _CanShowCommitError();

        /// <summary>
        /// Gets whether the error-menu separator should appear (any Show * Error item visible).
        /// </summary>
        public bool CanShowRowErrorMenu => CanShowCommitError || CanShowPreviewError || CanShowLoadErrors;

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

        /// <summary>
        /// Shows the last preview failure for the selected row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowPreviewError))]
        public void ShowPreviewError()
        {
            if (!_CanShowPreviewError())
            {
                return;
            }

            var entry = _selectedEntries[0];
            var previewError = entry.EngineItem.PreviewError;
            if (previewError is null)
            {
                return;
            }

            RowErrorDialogRequested?.Invoke(
                this,
                RenameListRowErrorDisplay.CreatePreview(
                    entry.FullPath,
                    previewError.Message,
                    RenameListRowErrorDisplay.FormatExceptionDetails(previewError.Cause)
                )
            );
        }

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
                RenameListRowErrorDisplay.CreateCommit(
                    entry.FullPath,
                    commitError.Message,
                    RenameListRowErrorDisplay.FormatExceptionDetails(commitError.Cause)
                )
            );
        }

        private bool _CanShowLoadErrors()
        {
            return _CanShowSingleRowError(static entry => entry.HasLoadError);
        }

        private bool _CanShowPreviewError()
        {
            return _CanShowSingleRowError(static entry => entry.HasPreviewError);
        }

        private bool _CanShowCommitError()
        {
            return _CanShowSingleRowError(static entry => entry.HasCommitError);
        }

        /// <summary>
        /// True when not busy, exactly one row is selected, and <paramref name="hasError"/> holds.
        /// </summary>
        private bool _CanShowSingleRowError(Func<RenameListEntry, bool> hasError)
        {
            if (IsBusy || _selectedEntries.Count != 1)
            {
                return false;
            }

            return hasError(_selectedEntries[0]);
        }

        /// <summary>
        /// Notifies all Show * Error menu visibility and command CanExecute state.
        /// </summary>
        private void _NotifyRowErrorCommandsChanged()
        {
            OnPropertyChanged(nameof(CanShowCommitError));
            OnPropertyChanged(nameof(CanShowPreviewError));
            OnPropertyChanged(nameof(CanShowLoadErrors));
            OnPropertyChanged(nameof(CanShowRowErrorMenu));
            ShowCommitErrorCommand.NotifyCanExecuteChanged();
            ShowPreviewErrorCommand.NotifyCanExecuteChanged();
            ShowLoadErrorsCommand.NotifyCanExecuteChanged();
        }
    }
}
