using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Show Preview Error command for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the user requests the Show Preview Error dialog.
        /// </summary>
        public event EventHandler<RenameListPreviewErrorDialogContent>? PreviewErrorDialogRequested;

        /// <summary>
        /// Gets whether Show Preview Error should appear on the row context menu.
        /// </summary>
        public bool CanShowPreviewError => _CanShowPreviewError();

        /// <summary>
        /// Gets whether the error-menu separator should appear (any Show * Error item visible).
        /// </summary>
        public bool CanShowRowErrorMenu => CanShowPreviewError || CanShowLoadErrors;

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

            PreviewErrorDialogRequested?.Invoke(
                this,
                new RenameListPreviewErrorDialogContent(
                    entry.FullPath,
                    previewError.Message,
                    previewError.Cause?.ToString()
                )
            );
        }

        private bool _CanShowPreviewError()
        {
            if (IsBusy || _selectedEntries.Count != 1)
            {
                return false;
            }

            return _selectedEntries[0].HasPreviewError;
        }

        private void _NotifyShowPreviewErrorChanged()
        {
            OnPropertyChanged(nameof(CanShowPreviewError));
            OnPropertyChanged(nameof(CanShowRowErrorMenu));
            ShowPreviewErrorCommand.NotifyCanExecuteChanged();
        }
    }
}
