using Mfr.Engine.Commit;
using Mfr.Models.Filters;

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
            var confirm = UiHooks?.ConfirmPreviewErrorsAsync;
            if (confirm is null)
            {
                return false;
            }

            return await confirm(errorCount).ConfigureAwait(true);
        }

        private async Task _ShowCommitErrorSummaryAsync(int errorCount)
        {
            var showSummary = UiHooks?.ShowCommitErrorSummaryAsync;
            if (showSummary is null)
            {
                return;
            }

            await showSummary(errorCount).ConfigureAwait(true);
        }
    }
}
