using Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Shared cancelable progress runner for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Runs cancelable Rename List background work through the shared progress dialog.
        /// </summary>
        /// <param name="operation">Dialog copy and phase for this run.</param>
        /// <param name="work">Engine work invoked with the operation cancel token and progress sink.</param>
        /// <param name="onCancel">
        /// Optional UI-thread callback when the run was plain-canceled or refused because another
        /// operation is busy (e.g. rollback add, disable Auto-Preview). Not invoked for
        /// <see cref="RenameListProgressResult.CanceledKeep"/>. Omit for no-op cancel (refresh / metadata).
        /// </param>
        /// <param name="addCancelDisposition">
        /// Optional Add cancel disposition so Keep added can set KeepPartial before canceling.
        /// </param>
        /// <returns>
        /// Completed when work finished; Canceled when canceled/busy-refused; CanceledKeep when Add
        /// was stopped with Keep added.
        /// </returns>
        private async Task<RenameListProgressResult> _RunProgressAsync(
            RenameListProgressOperation operation,
            Action<CancellationToken, IProgress<RenameListProgress>> work,
            Action? onCancel = null,
            RenameListAddCancelDisposition? addCancelDisposition = null
        )
        {
            ArgumentNullException.ThrowIfNull(work);

            if (IsBusy)
            {
                return RenameListProgressResult.Canceled;
            }

            var result = await Progress.RunAsync(operation, work, addCancelDisposition).ConfigureAwait(true);
            if (result == RenameListProgressResult.Canceled)
            {
                onCancel?.Invoke();
            }

            return result;
        }
    }
}
