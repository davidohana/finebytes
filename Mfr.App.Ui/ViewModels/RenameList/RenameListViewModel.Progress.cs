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
        /// Optional UI-thread callback when the run was canceled or refused because another operation is busy
        /// (e.g. rollback add, disable Auto-Preview). Omit for no-op cancel (refresh / metadata).
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the work finished; <see langword="false"/> when canceled or when
        /// another operation was already running.
        /// </returns>
        private async Task<bool> _RunProgressAsync(
            RenameListProgressOperation operation,
            Action<CancellationToken, IProgress<RenameListProgress>> work,
            Action? onCancel = null
        )
        {
            ArgumentNullException.ThrowIfNull(work);

            if (IsBusy)
            {
                return false;
            }

            var completed = await Progress.RunAsync(operation, work).ConfigureAwait(true);
            if (!completed)
            {
                onCancel?.Invoke();
            }

            return completed;
        }
    }
}
