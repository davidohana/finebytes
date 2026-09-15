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

            RenameListPrepareUndoResult? prepareResult = null;
            IReadOnlyList<RenameResultItem>? results = null;
            var commitCompleted = await _RunProgressAsync(
                    RenameListProgressOperation.Commit,
                    (token, progress) =>
                    {
                        prepareResult = _renameList.PrepareUndo(log, cancellationToken: token, progress: progress);
                        results = _renameList.Commit(
                            prepareResult.Plan,
                            failFast: false,
                            dryRun: false,
                            cancellationToken: token,
                            progress: progress
                        );
                    }
                )
                .ConfigureAwait(true);

            _ReplaceEntriesFromEngine();
            _ClearPreviewCounts();
            _RefreshFieldDisplay();

            var undoneCount = results?.Count(item => item.Status == RenameStatus.CommitOk) ?? 0;
            var commitErrorCount = results?.Count(item => item.Status == RenameStatus.CommitError) ?? 0;
            var notLoadedCount = prepareResult?.NotLoadedCount ?? 0;
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
            if (!stopped && undoneCount == 0 && errorCount == 0 && notLoadedCount > 0)
            {
                return StatusBarText.Warning($"Could not load {notLoadedCount} item(s) for undo (paths missing).");
            }

            var primary = _FormatSuccessPrimary(successCount: undoneCount, stopped: stopped, successPastVerb: "Undid");
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

            return _CombineStatusParts(parts);
        }

        /// <summary>
        /// Builds the stopped or success primary fragment shared by GO and Undo status lines.
        /// </summary>
        /// <param name="successCount">CommitOk count (renamed / undone).</param>
        /// <param name="stopped">Whether the operation was canceled mid-commit.</param>
        /// <param name="successPastVerb">Past-tense verb (<c>Renamed</c> / <c>Undid</c>).</param>
        /// <returns>
        /// Warning when stopped; Neutral success when <paramref name="successCount"/> &gt; 0 and not stopped;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static StyledTextDisplay? _FormatSuccessPrimary(int successCount, bool stopped, string successPastVerb)
        {
            if (stopped)
            {
                return successCount > 0
                    ? StatusBarText.Warning($"Stopped. {successPastVerb} {successCount} item(s).")
                    : StatusBarText.Warning("Stopped.");
            }

            if (successCount > 0)
            {
                return StatusBarText.Neutral($"{successPastVerb} {successCount} item(s).");
            }

            return null;
        }

        /// <summary>
        /// Space-joins non-empty status fragments (single part returned as-is).
        /// </summary>
        /// <param name="parts">Ordered fragments to combine.</param>
        /// <returns>Combined display.</returns>
        private static StyledTextDisplay _CombineStatusParts(List<StyledTextDisplay> parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            if (parts.Count == 0)
            {
                return StatusBarText.Neutral(string.Empty);
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
