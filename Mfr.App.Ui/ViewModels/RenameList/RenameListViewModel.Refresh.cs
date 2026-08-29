using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Original Refresh (re-read disk fields; no preview) for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Gets whether the Rename List grid currently has keyboard focus.
        /// </summary>
        public bool IsGridFocused { get; private set; }

        /// <summary>
        /// Re-reads original fields from disk for every row, then hydrates metadata for visible columns and sort keys.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRefresh))]
        public async Task RefreshAsync()
        {
            if (!_CanRefresh())
            {
                return;
            }

            var requirement = _CurrentMetadataRequirement();
            var completed = await AddProgress
                .RunAsync(
                    RenameListProgressOperation.Refresh,
                    (token, progress) =>
                    {
                        _renameList.RefreshOriginals(token, progress);
                        if (!token.IsCancellationRequested)
                        {
                            _renameList.EnsureMetadataLoaded(requirement, token, progress);
                        }
                    }
                )
                .ConfigureAwait(true);
            if (!completed)
            {
                return;
            }

            if (IsAutoSort && _sortKeys.Count > 0 && _renameList.Sort(_sortKeys))
            {
                _SyncEntriesToEngineOrder();
            }

            _RefreshFieldDisplay();
        }

        /// <summary>
        /// Updates grid-focus state used to route F5 between File List and Rename List.
        /// </summary>
        /// <param name="focused">Whether the Rename List grid has focus.</param>
        internal void SetGridFocused(bool focused)
        {
            if (IsGridFocused == focused)
            {
                return;
            }

            IsGridFocused = focused;
        }

        private bool _CanRefresh()
        {
            return !IsAdding && Entries.Count > 0;
        }

        private void _NotifyRefreshChanged()
        {
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }
}
