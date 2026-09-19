using Mfr.Engine.Config;
using Mfr.Engine.RenameList;
using Mfr.Engine.RenameLog;
using Mfr.Models.Config;
using Mfr.Models.Filters;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Undo Last / Log-window Undo orchestration for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Confirms (when policy requires) and prepares an undo session from the last GO (no Commit).
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when prepare finished (including zero loaded rows); <see langword="false"/> when
        /// there was nothing to undo, confirm was declined, or the operation was refused while busy.
        /// </returns>
        public Task<bool> PrepareUndoLastAsync()
        {
            var log = RenameLogStore.LastOperation;
            if (log is null || !log.HasUndoableEntries)
            {
                if (IsBusy)
                {
                    return Task.FromResult(false);
                }

                LastStatusMessage = StatusBarText.Warning("Nothing to undo.");
                return Task.FromResult(false);
            }

            return PrepareUndoAsync(log);
        }

        /// <summary>
        /// Confirms (when policy requires), rebuilds the list from <paramref name="log"/> as a preview-only
        /// undo session, and clears Filter Chain. User presses GO to apply.
        /// </summary>
        /// <param name="log">Rename operation to reverse (last op or a loaded disk log).</param>
        /// <returns>
        /// <see langword="true"/> when prepare finished (including zero loaded rows); <see langword="false"/> when
        /// there was nothing to undo, confirm was declined, or the operation was refused while busy.
        /// </returns>
        public async Task<bool> PrepareUndoAsync(RenameLog log)
        {
            ArgumentNullException.ThrowIfNull(log);

            if (IsBusy)
            {
                return false;
            }

            if (!log.HasUndoableEntries)
            {
                LastStatusMessage = StatusBarText.Warning("Nothing to undo.");
                return false;
            }

            if (!await _ConfirmUndoRenameAsync().ConfigureAwait(true))
            {
                return false;
            }

            using (SuspendPreviewInputs())
            {
                RenameListPrepareUndoResult? prepareResult = null;
                var prepareResultStatus = await _RunProgressAsync(
                        RenameListProgressOperation.Add,
                        (token, progress) =>
                        {
                            prepareResult = _renameList.PrepareUndo(log, cancellationToken: token, progress: progress);
                        }
                    )
                    .ConfigureAwait(true);

                // Clear filters before replacing Entries so a later Auto-Preview sees an empty chain
                // (not the pre-undo filters). Undo confirm already warned filters will be cleared.
                _filterChain?.ReplaceFromChain(new FilterChain { Steps = [] });
                _ReplaceEntriesFromEngine();

                if (prepareResult is not null)
                {
                    _ApplyPreviewPlan(prepareResult.Plan);
                }
                else
                {
                    _ClearPreviewCounts();
                    _RefreshFieldDisplay();
                }

                await _ReplaceVisibleColumnsForUndoAsync(log).ConfigureAwait(true);

                var preparedCount = prepareResult?.PreparedCount ?? 0;
                var notLoadedCount = prepareResult?.NotLoadedCount ?? 0;
                LastStatusMessage = _FormatPrepareUndoOutcome(
                    preparedCount: preparedCount,
                    notLoadedCount: notLoadedCount,
                    stopped: prepareResultStatus != RenameListProgressResult.Completed
                );
            }

            return true;
        }

        /// <summary>
        /// Replaces visible columns with catalog defaults, then appends keys for properties changed in the undo log.
        /// </summary>
        /// <param name="log">Prepared undo log (undoable entries only contribute mapped keys).</param>
        /// <remarks>
        /// <para>
        /// Same path as <see cref="ReplaceWithRelevantColumnsAsync"/>: defaults first, then missing relevant keys,
        /// originals-only normalize while Before/After Mode is on (A/B stays enabled; Preview side shows companions).
        /// Widths for keys that were already visible are preserved.
        /// </para>
        /// </remarks>
        private async Task _ReplaceVisibleColumnsForUndoAsync(RenameLog log)
        {
            var undoKeys = RenamePropertyFileMeta.CollectPreviewKeysFromLog(log);
            if (IsAbModeEnabled)
            {
                undoKeys = RenameListVisibleColumn.ToOriginalKeysFirstSeen(undoKeys);
            }

            await _ApplyVisibleColumnsWithHydrateAsync(_BuildDefaultsThenRelevantColumns(undoKeys))
                .ConfigureAwait(true);
        }

        private async Task<bool> _ConfirmUndoRenameAsync()
        {
            if (!ConfirmationPolicy.ShouldConfirm(ConfirmationKind.UndoRename))
            {
                return true;
            }

            var confirm = UiHooks?.ConfirmUndoRenameAsync;
            if (confirm is null)
            {
                return false;
            }

            return await confirm().ConfigureAwait(true);
        }

        /// <summary>
        /// Rebuilds <see cref="Entries"/> to match the engine after PrepareUndo replaces the list.
        /// </summary>
        private void _ReplaceEntriesFromEngine()
        {
            Entries.ReplaceAll(_renameList.RenameItems.Select(RenameListEntry.ToEntry));
            SetSelectedEntries([]);
            SetDropMarkIndex(null);
            _NotifyMembershipChanged();
        }

        /// <summary>
        /// Builds the status-bar message after PrepareUndo (or Stop mid-prepare).
        /// </summary>
        private static StyledTextDisplay _FormatPrepareUndoOutcome(int preparedCount, int notLoadedCount, bool stopped)
        {
            if (!stopped && preparedCount == 0 && notLoadedCount > 0)
            {
                return StatusBarText.Warning($"Could not load {notLoadedCount} item(s) for undo (paths missing).");
            }

            var parts = new List<StyledTextDisplay>();
            if (stopped)
            {
                parts.Add(
                    preparedCount > 0
                        ? StatusBarText.Warning($"Stopped. Prepared undo of {preparedCount} item(s).")
                        : StatusBarText.Warning("Stopped.")
                );
            }
            else if (preparedCount > 0)
            {
                parts.Add(StatusBarText.Neutral($"Prepared undo of {preparedCount} item(s) — press GO to apply."));
            }

            if (notLoadedCount > 0)
            {
                parts.Add(StatusBarText.Warning($"{notLoadedCount} item(s) could not be loaded."));
            }

            if (parts.Count == 0)
            {
                return StatusBarText.Neutral("No items were prepared for undo.");
            }

            return _CombineStatusParts(parts);
        }
    }
}
