using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Original Refresh (re-read disk fields) for <see cref="RenameListViewModel"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Does not run preview itself. Raises <see cref="OriginalsRefreshed"/> so the shell can re-preview
    /// when Auto-Preview is on (MFR7 full F5 refresh).
    /// </para>
    /// </remarks>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Whether the Rename List grid currently has keyboard focus (F5 routing vs File List).
        /// </summary>
        [ObservableProperty]
        private bool _isGridFocused;

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
            var completed = await _RunProgressAsync(
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
            var refreshStatus = _FormatRefreshOutcome();
            if (!refreshStatus.IsEmpty)
            {
                LastStatusMessage = refreshStatus;
            }

            OriginalsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Builds the status-bar message after Refresh when load errors remain; silent when clean.
        /// </summary>
        private StyledTextDisplay _FormatRefreshOutcome()
        {
            var itemsWithLoadErrors = Entries.Count(entry => entry.HasRowError);
            if (itemsWithLoadErrors == 0)
            {
                return StyledTextDisplay.Empty;
            }

            return StatusBarText.Warning(
                $"Refreshed {Entries.Count} item(s). {itemsWithLoadErrors} item(s) with load error(s) — select a cell for details."
            );
        }

        private bool _CanRefresh()
        {
            return !IsBusy && Entries.Count > 0;
        }

        private void _NotifyRefreshChanged()
        {
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }
}
