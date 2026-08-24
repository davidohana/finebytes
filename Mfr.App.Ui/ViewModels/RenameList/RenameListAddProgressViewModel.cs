using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Tracks a background Rename List add: progress counts, delayed dialog visibility, and cancel.
    /// </summary>
    public sealed partial class RenameListAddProgressViewModel : ViewModelBase
    {
        private const int DialogDelayMilliseconds = 200;

        private CancellationTokenSource? _cts;

        /// <summary>
        /// Gets whether an add operation is running.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        private bool _isAdding;

        /// <summary>
        /// Gets whether the progress dialog should be shown.
        /// </summary>
        /// <para>
        /// Stays false until the add has been running longer than a short delay, so fast adds never flash a dialog.
        /// </para>
        [ObservableProperty]
        private bool _isDialogVisible;

        /// <summary>
        /// Gets how many filesystem entries have been scanned during the current add.
        /// </summary>
        [ObservableProperty]
        private int _scannedCount;

        /// <summary>
        /// Gets how many items have been accepted during the current add.
        /// </summary>
        [ObservableProperty]
        private int _addedCount;

        /// <summary>
        /// Gets the most recent path considered during the current add.
        /// </summary>
        [ObservableProperty]
        private string _lastPath = string.Empty;

        /// <summary>
        /// Requests cancel for the in-progress add. The engine stops the walk; the caller discards the batch.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanCancel))]
        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Runs add work on a background thread, reports progress, and shows the dialog only if it takes long enough.
        /// </summary>
        /// <param name="work">Engine add invoked with the operation cancel token and progress sink.</param>
        /// <returns>
        /// <see langword="true"/> when the work finished without user cancel; <see langword="false"/> when canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is <see langword="null"/>.</exception>
        internal async Task<bool> RunAsync(Action<CancellationToken, IProgress<RenameListAddProgress>> work)
        {
            ArgumentNullException.ThrowIfNull(work);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progress = new Progress<RenameListAddProgress>(_ApplyProgress);

            IsAdding = true;
            IsDialogVisible = false;
            ScannedCount = 0;
            AddedCount = 0;
            LastPath = string.Empty;

            var showDialogDelay = Task.Delay(DialogDelayMilliseconds, CancellationToken.None);
            var addTask = Task.Run(() => work(token, progress), token);

            var completed = await Task.WhenAny(addTask, showDialogDelay).ConfigureAwait(true);
            if (completed == showDialogDelay && !addTask.IsCompleted)
            {
                IsDialogVisible = true;
            }

            var canceled = false;
            try
            {
                await addTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Task.Run may still fault if cancel wins before the worker starts.
                canceled = true;
            }
            finally
            {
                // Engine stops the walk without throwing; treat a signaled token as user cancel.
                canceled = canceled || token.IsCancellationRequested;
                // Clear IsAdding before hiding the dialog so programmatic Close is not canceled.
                IsAdding = false;
                IsDialogVisible = false;
                _cts.Dispose();
                _cts = null;
            }

            return !canceled;
        }

        private void _ApplyProgress(RenameListAddProgress progress)
        {
            ScannedCount = progress.ScannedCount;
            AddedCount = progress.AddedCount;
            LastPath = progress.LastPath;
        }

        private bool _CanCancel()
        {
            return IsAdding;
        }
    }
}
