using Avalonia.Media;
using Mfr.Engine.Commit;
using Mfr.Models.Filters;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// GO preview and filesystem commit orchestration for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Clears old commit errors, previews the current chain, confirms preview errors, and commits valid rows.
        /// </summary>
        /// <param name="chain">Live Applied Filters chain to preview immediately before commit.</param>
        /// <returns>
        /// <see langword="true"/> when the commit stage was reached; <see langword="false"/> when GO was refused,
        /// preview was canceled, or the preview-error warning was declined.
        /// </returns>
        public async Task<bool> GoAsync(FilterChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (IsBusy || Entries.Count == 0)
            {
                return false;
            }

            LastStatusMessage = StyledTextDisplay.Empty;
            _renameList.ClearCommitErrors();
            _RefreshFieldDisplay();

            CommitPlan? plan = null;
            var previewCompleted = await _RunProgressAsync(
                    RenameListProgressOperation.Preview,
                    (token, progress) =>
                    {
                        plan = _renameList.Preview(chain, token, progress);
                    }
                )
                .ConfigureAwait(true);

            if (plan is not null)
            {
                _ApplyPreviewPlan(plan);
            }
            else
            {
                _RefreshFieldDisplay();
            }

            if (!previewCompleted || plan is null)
            {
                if (!previewCompleted)
                {
                    LastStatusMessage = StatusBarText.Warning("Stopped.");
                }

                return false;
            }

            if (plan.ErrorCount > 0 && !await _ConfirmPreviewErrorsAsync(plan.ErrorCount).ConfigureAwait(true))
            {
                return false;
            }

            IReadOnlyList<RenameResultItem>? results = null;
            var commitCompleted = await _RunProgressAsync(
                    RenameListProgressOperation.Commit,
                    (token, progress) =>
                    {
                        results = _renameList.Commit(
                            plan,
                            failFast: false,
                            cancellationToken: token,
                            progress: progress
                        );
                    }
                )
                .ConfigureAwait(true);

            _ClearPreviewCounts();
            _RefreshFieldDisplay();

            var renamedCount = results?.Count(item => item.Status == RenameStatus.CommitOk) ?? 0;
            var commitErrorCount = results?.Count(item => item.Status == RenameStatus.CommitError) ?? 0;
            LastStatusMessage = _FormatGoOutcome(
                renamedCount: renamedCount,
                errorCount: commitErrorCount,
                stopped: !commitCompleted
            );

            return true;
        }

        private async Task<bool> _ConfirmPreviewErrorsAsync(int errorCount)
        {
            var confirm = UiHooks?.ConfirmPreviewErrorsAsync;
            if (confirm is null)
            {
                return false;
            }

            return await confirm(errorCount).ConfigureAwait(true);
        }

        /// <summary>
        /// Builds the status-bar message after a GO commit (or Stop mid-commit).
        /// </summary>
        private static StyledTextDisplay _FormatGoOutcome(int renamedCount, int errorCount, bool stopped)
        {
            if (stopped)
            {
                var stoppedPart =
                    renamedCount > 0
                        ? StatusBarText.Warning($"Stopped. Renamed {renamedCount} item(s).")
                        : StatusBarText.Warning("Stopped.");
                if (errorCount == 0)
                {
                    return stoppedPart;
                }

                return StatusBarText.Combine(
                    stoppedPart,
                    StatusBarText.Neutral(" "),
                    _FormatCouldNotRenameHint(errorCount, itemSuffix: false)
                );
            }

            if (errorCount > 0 && renamedCount > 0)
            {
                return StatusBarText.Combine(
                    StatusBarText.Neutral($"Renamed {renamedCount} item(s). "),
                    _FormatCouldNotRenameHint(errorCount, itemSuffix: false)
                );
            }

            if (errorCount > 0)
            {
                return _FormatCouldNotRenameHint(errorCount, itemSuffix: true);
            }

            if (renamedCount > 0)
            {
                return StatusBarText.Neutral($"Renamed {renamedCount} item(s).");
            }

            return StatusBarText.Neutral("No items were renamed.");
        }

        /// <summary>
        /// Builds the error fragment that points at the row menu command (command name in bold).
        /// </summary>
        private static StyledTextDisplay _FormatCouldNotRenameHint(int errorCount, bool itemSuffix)
        {
            var leading = itemSuffix
                ? $"{errorCount} item(s) could not be renamed. Right-click "
                : $"{errorCount} could not be renamed — right-click ";
            var trailing = itemSuffix ? " for details." : ".";

            return StatusBarText.Combine(
                StatusBarText.Error(leading),
                StyledTextDisplay.FromRuns(
                    new StyledTextRun("Show Rename Error")
                    {
                        FontWeight = FontWeight.Bold,
                        ForegroundResourceKey = StatusBarText.ErrorForegroundResourceKey,
                    }
                ),
                StatusBarText.Error(trailing)
            );
        }
    }
}
