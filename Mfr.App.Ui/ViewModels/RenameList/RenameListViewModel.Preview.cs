using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.Commit;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Filter-chain preview for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Count of items whose preview differs from the original after the last preview pass.
        /// </summary>
        [ObservableProperty]
        private int _changeCount;

        /// <summary>
        /// Count of items with a preview error after the last preview pass.
        /// </summary>
        [ObservableProperty]
        private int _previewErrorCount;

        /// <summary>
        /// Gets whether filter-chain and membership changes automatically re-run preview (MFR7 Auto-Preview).
        /// </summary>
        [ObservableProperty]
        private bool _isAutoPreview = true;

        /// <summary>
        /// Toggles Auto-Preview. Turning it on notifies so the shell re-previews.
        /// </summary>
        [RelayCommand]
        public void ToggleAutoPreview()
        {
            IsAutoPreview = !IsAutoPreview;
        }

        /// <summary>
        /// Turns Auto-Preview off (MFR7: canceling a long preview disables auto-preview).
        /// </summary>
        public void DisableAutoPreview()
        {
            if (!IsAutoPreview)
            {
                return;
            }

            IsAutoPreview = false;
        }

        /// <summary>
        /// Runs the filter chain over every Rename List item and refreshes preview columns.
        /// </summary>
        /// <param name="chain">Live Applied Filters chain.</param>
        /// <remarks>
        /// <para>
        /// Synchronous path for tests and callers that already own the UI thread. Prefer
        /// <see cref="PreviewAsync"/> from the shell so long runs show cancelable progress.
        /// </para>
        /// </remarks>
        public void Preview(FilterChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (Entries.Count == 0)
            {
                _ClearPreviewCounts();
                return;
            }

            _ApplyPreviewPlan(_renameList.Preview(chain));
        }

        /// <summary>
        /// Runs preview on a background thread with delayed cancelable progress (MFR7 PreviewProgressDialog).
        /// </summary>
        /// <param name="chain">Live Applied Filters chain.</param>
        /// <returns>
        /// <see langword="true"/> when preview finished; <see langword="false"/> when canceled
        /// (Auto-Preview is then disabled).
        /// </returns>
        public async Task<bool> PreviewAsync(FilterChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (Entries.Count == 0)
            {
                _ClearPreviewCounts();
                return true;
            }

            // Busy refuse must not disable Auto-Preview (unlike user cancel).
            if (IsBusy)
            {
                return true;
            }

            CommitPlan? plan = null;
            var completed = await _RunProgressAsync(
                    RenameListProgressOperation.Preview,
                    (token, progress) =>
                    {
                        plan = _renameList.Preview(chain, token, progress);
                    },
                    onCancel: DisableAutoPreview
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

            return completed;
        }

        private void _ClearPreviewCounts()
        {
            ChangeCount = 0;
            PreviewErrorCount = 0;
        }

        /// <summary>
        /// Copies engine preview outcome counts onto the grid after a preview pass.
        /// </summary>
        private void _ApplyPreviewPlan(CommitPlan plan)
        {
            ChangeCount = plan.ChangedCount;
            PreviewErrorCount = plan.ErrorCount;
            _RefreshFieldDisplay();
        }
    }
}
