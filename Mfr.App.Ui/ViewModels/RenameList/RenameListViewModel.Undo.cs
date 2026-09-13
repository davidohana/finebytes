using Mfr.Engine.RenameLog;
using Mfr.Models.Config;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Undo Last orchestration for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Confirms (when policy requires), rebuilds the list from the last GO, and re-commits OldValues.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the undo commit stage was reached; <see langword="false"/> when there was
        /// nothing to undo, confirm was declined, or the operation was refused while busy.
        /// </returns>
        public async Task<bool> UndoLastAsync()
        {
            if (IsBusy)
            {
                return false;
            }

            var log = RenameLogStore.LastOperation;
            if (log is null || !log.HasUndoableEntries)
            {
                LastStatusMessage = StatusBarText.Warning("Nothing to undo.");
                return false;
            }

            if (!await _ConfirmUndoRenameAsync().ConfigureAwait(true))
            {
                return false;
            }

            IReadOnlyList<RenameResultItem>? results = null;
            var commitCompleted = await _RunProgressAsync(
                    RenameListProgressOperation.Commit,
                    (token, progress) =>
                    {
                        results = _renameList.Undo(log, failFast: false, cancellationToken: token, progress: progress);
                    }
                )
                .ConfigureAwait(true);

            _ReplaceEntriesFromEngine();
            _ClearPreviewCounts();
            _RefreshFieldDisplay();

            var undoneCount = results?.Count(item => item.Status == RenameStatus.CommitOk) ?? 0;
            var commitErrorCount = results?.Count(item => item.Status == RenameStatus.CommitError) ?? 0;
            LastStatusMessage = _FormatUndoOutcome(
                undoneCount: undoneCount,
                errorCount: commitErrorCount,
                stopped: !commitCompleted
            );

            return true;
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
        /// Rebuilds <see cref="Entries"/> to match the engine after Undo replaces the list.
        /// </summary>
        private void _ReplaceEntriesFromEngine()
        {
            Entries.ReplaceAll(_renameList.RenameItems.Select(RenameListEntry.ToEntry));
            SetSelectedEntries([]);
            SetDropMarkIndex(null);
            _NotifyMembershipChanged();
        }

        /// <summary>
        /// Builds the status-bar message after an Undo commit (or Stop mid-undo).
        /// </summary>
        private static StyledTextDisplay _FormatUndoOutcome(int undoneCount, int errorCount, bool stopped)
        {
            if (stopped)
            {
                var stoppedPart =
                    undoneCount > 0
                        ? StatusBarText.Warning($"Stopped. Undid {undoneCount} item(s).")
                        : StatusBarText.Warning("Stopped.");
                if (errorCount == 0)
                {
                    return stoppedPart;
                }

                return StatusBarText.Combine(
                    stoppedPart,
                    StatusBarText.Neutral(" "),
                    StatusBarText.Error($"{errorCount} error(s) during undo.")
                );
            }

            if (errorCount > 0 && undoneCount > 0)
            {
                return StatusBarText.Combine(
                    StatusBarText.Neutral($"Undid {undoneCount} item(s). "),
                    StatusBarText.Error($"{errorCount} error(s) during undo.")
                );
            }

            if (errorCount > 0)
            {
                return StatusBarText.Error($"{errorCount} error(s) during undo.");
            }

            if (undoneCount > 0)
            {
                return StatusBarText.Neutral($"Undid {undoneCount} item(s).");
            }

            return StatusBarText.Neutral("No items were undone.");
        }
    }
}
