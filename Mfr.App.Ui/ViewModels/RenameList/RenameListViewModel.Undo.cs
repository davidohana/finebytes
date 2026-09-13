using Mfr.Engine.RenameList;
using Mfr.Engine.RenameLog;
using Mfr.Models.Config;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Undo Last / Log-window Undo orchestration for <see cref="RenameListViewModel"/>.
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
        public Task<bool> UndoLastAsync()
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

            return UndoAsync(log);
        }

        /// <summary>
        /// Confirms (when policy requires), rebuilds the list from <paramref name="log"/>, and re-commits OldValues.
        /// </summary>
        /// <param name="log">Rename operation to reverse (last op or a loaded disk log).</param>
        /// <returns>
        /// <see langword="true"/> when the undo commit stage was reached; <see langword="false"/> when there was
        /// nothing to undo, confirm was declined, or the operation was refused while busy.
        /// </returns>
        public async Task<bool> UndoAsync(RenameLog log)
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

            RenameListUndoResult? undoResult = null;
            var commitCompleted = await _RunProgressAsync(
                    RenameListProgressOperation.Commit,
                    (token, progress) =>
                    {
                        undoResult = _renameList.Undo(
                            log,
                            failFast: false,
                            cancellationToken: token,
                            progress: progress
                        );
                    }
                )
                .ConfigureAwait(true);

            _ReplaceEntriesFromEngine();
            _ClearPreviewCounts();
            _RefreshFieldDisplay();

            var results = undoResult?.Results;
            var undoneCount = results?.Count(item => item.Status == RenameStatus.CommitOk) ?? 0;
            var commitErrorCount = results?.Count(item => item.Status == RenameStatus.CommitError) ?? 0;
            var notLoadedCount = undoResult?.NotLoadedCount ?? 0;
            LastStatusMessage = _FormatUndoOutcome(
                undoneCount: undoneCount,
                errorCount: commitErrorCount,
                notLoadedCount: notLoadedCount,
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
        private static StyledTextDisplay _FormatUndoOutcome(
            int undoneCount,
            int errorCount,
            int notLoadedCount,
            bool stopped
        )
        {
            if (stopped)
            {
                return _CombineUndoParts(
                    primary: undoneCount > 0
                        ? StatusBarText.Warning($"Stopped. Undid {undoneCount} item(s).")
                        : StatusBarText.Warning("Stopped."),
                    errorCount: errorCount,
                    notLoadedCount: notLoadedCount
                );
            }

            if (undoneCount == 0 && errorCount == 0 && notLoadedCount > 0)
            {
                return StatusBarText.Warning($"Could not load {notLoadedCount} item(s) for undo (paths missing).");
            }

            if (undoneCount == 0 && errorCount == 0 && notLoadedCount == 0)
            {
                return StatusBarText.Neutral("No items were undone.");
            }

            StyledTextDisplay? primary = null;
            if (undoneCount > 0)
            {
                primary = StatusBarText.Neutral($"Undid {undoneCount} item(s).");
            }

            return _CombineUndoParts(primary: primary, errorCount: errorCount, notLoadedCount: notLoadedCount);
        }

        private static StyledTextDisplay _CombineUndoParts(
            StyledTextDisplay? primary,
            int errorCount,
            int notLoadedCount
        )
        {
            var parts = new List<StyledTextDisplay>();
            if (primary is not null)
            {
                parts.Add(primary);
            }

            if (errorCount > 0)
            {
                parts.Add(StatusBarText.Error($"{errorCount} error(s) during undo."));
            }

            if (notLoadedCount > 0)
            {
                parts.Add(StatusBarText.Warning($"{notLoadedCount} item(s) could not be loaded."));
            }

            if (parts.Count == 0)
            {
                return StatusBarText.Neutral("No items were undone.");
            }

            if (parts.Count == 1)
            {
                return parts[0];
            }

            var combined = parts[0];
            for (var i = 1; i < parts.Count; i++)
            {
                combined = StatusBarText.Combine(combined, StatusBarText.Neutral(" "), parts[i]);
            }

            return combined;
        }
    }
}
