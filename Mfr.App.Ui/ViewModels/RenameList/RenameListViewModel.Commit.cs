using Mfr.Engine.Commit;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Handles a request to confirm continuing GO when preview errors are present.
    /// </summary>
    /// <param name="sender">Rename List view model raising the request.</param>
    /// <param name="errorCount">Number of rows that GO will ignore because preview failed.</param>
    /// <returns><see langword="true"/> to continue to commit; otherwise <see langword="false"/>.</returns>
    public delegate Task<bool> RenameListPreviewErrorConfirmationHandler(object? sender, int errorCount);

    /// <summary>
    /// Handles a request to show the post-GO commit-error summary.
    /// </summary>
    /// <param name="sender">Rename List view model raising the request.</param>
    /// <param name="errorCount">Number of rows whose commit failed.</param>
    /// <returns>A task that completes when the summary closes.</returns>
    public delegate Task RenameListCommitErrorSummaryHandler(object? sender, int errorCount);

    /// <summary>
    /// GO preview and filesystem commit orchestration for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when GO needs confirmation before ignoring rows with preview errors.
        /// </summary>
        public event RenameListPreviewErrorConfirmationHandler? PreviewErrorConfirmationRequested;

        /// <summary>
        /// Raised after GO when one or more rows could not be committed.
        /// </summary>
        public event RenameListCommitErrorSummaryHandler? CommitErrorSummaryRequested;

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
                return false;
            }

            if (plan.ErrorCount > 0 && !await _ConfirmPreviewErrorsAsync(plan.ErrorCount).ConfigureAwait(true))
            {
                return false;
            }

            await _RunProgressAsync(
                    RenameListProgressOperation.Commit,
                    (token, progress) =>
                    {
                        _renameList.Commit(plan, failFast: false, cancellationToken: token, progress: progress);
                    }
                )
                .ConfigureAwait(true);

            _ClearPreviewCounts();
            _RefreshFieldDisplay();

            var commitErrorCount = Entries.Count(entry => entry.HasCommitError);
            if (commitErrorCount > 0)
            {
                await _ShowCommitErrorSummaryAsync(commitErrorCount).ConfigureAwait(true);
            }

            return true;
        }

        private async Task<bool> _ConfirmPreviewErrorsAsync(int errorCount)
        {
            var handlers = PreviewErrorConfirmationRequested;
            if (handlers is null)
            {
                return false;
            }

            foreach (var handler in handlers.GetInvocationList().Cast<RenameListPreviewErrorConfirmationHandler>())
            {
                if (!await handler(this, errorCount).ConfigureAwait(true))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task _ShowCommitErrorSummaryAsync(int errorCount)
        {
            var handlers = CommitErrorSummaryRequested;
            if (handlers is null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList().Cast<RenameListCommitErrorSummaryHandler>())
            {
                await handler(this, errorCount).ConfigureAwait(true);
            }
        }
    }
}
