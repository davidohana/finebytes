using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Tracks a background Rename List operation: progress counts, delayed dialog visibility, and cancel.
    /// </summary>
    public sealed partial class RenameListProgressViewModel : ViewModelBase
    {
        private const int DialogDelayMilliseconds = 500;

        private CancellationTokenSource? _cts;

        /// <summary>
        /// Gets whether a background operation is running.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        private bool _isBusy;

        /// <summary>
        /// Gets whether the progress dialog should be shown.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Stays false until the operation has been running longer than a short delay, so fast work never flashes a dialog.
        /// </para>
        /// </remarks>
        [ObservableProperty]
        private bool _isDialogVisible;

        /// <summary>
        /// Gets the active background operation kind.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowResolveProgress))]
        [NotifyPropertyChangedFor(nameof(DialogTitle))]
        [NotifyPropertyChangedFor(nameof(MetadataProgressText))]
        private RenameListProgressOperation _operation = RenameListProgressOperation.Add;

        /// <summary>
        /// Gets the current stage reported by the engine.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DialogTitle))]
        [NotifyPropertyChangedFor(nameof(ShowMetadataProgress))]
        private RenameListProgressPhase _phase = RenameListProgressPhase.ResolveSources;

        /// <summary>
        /// Gets how many filesystem entries have been scanned during resolve.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PrimaryProgressText))]
        private int _scannedCount;

        /// <summary>
        /// Gets how many items have been accepted during resolve.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SecondaryProgressText))]
        private int _addedCount;

        /// <summary>
        /// Gets how many rows have been processed during metadata hydrate or preview.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MetadataProgressText))]
        private int _metadataProcessedCount;

        /// <summary>
        /// Gets the total row count for metadata/preview work; zero during resolve.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MetadataProgressText))]
        [NotifyPropertyChangedFor(nameof(ShowProgressBar))]
        private int _metadataTotalCount;

        /// <summary>
        /// Gets the most recent path considered during the current operation.
        /// </summary>
        [ObservableProperty]
        private string _lastPath = string.Empty;

        /// <summary>
        /// Gets the progress dialog window title for the current operation.
        /// </summary>
        public string DialogTitle => RenameListProgressCopy.DialogTitle(Operation, Phase);

        /// <summary>
        /// Gets the resolve-stage scan line for add operations.
        /// </summary>
        public string PrimaryProgressText => $"Scanned {ScannedCount} files";

        /// <summary>
        /// Gets the resolve-stage add line for add operations.
        /// </summary>
        public string SecondaryProgressText => $"Added {AddedCount} files";

        /// <summary>
        /// Gets the per-row progress line shown for metadata, refresh, or preview.
        /// </summary>
        public string MetadataProgressText =>
            RenameListProgressCopy.MetadataProgressText(Operation, MetadataProcessedCount, MetadataTotalCount);

        /// <summary>
        /// Gets whether scanned/added resolve lines should be shown.
        /// </summary>
        public bool ShowResolveProgress => RenameListProgressCopy.For(Operation).ShowResolve;

        /// <summary>
        /// Gets whether the per-row progress line should be shown.
        /// </summary>
        public bool ShowMetadataProgress => Phase == RenameListProgressPhase.LoadMetadata;

        /// <summary>
        /// Gets whether the determinate progress bar should be shown.
        /// </summary>
        /// <remarks>
        /// <para>
        /// True only when the engine has reported a known total (metadata, refresh, preview, or add's
        /// metadata stage). Hidden during resolve because the filesystem walk length is unknown.
        /// </para>
        /// </remarks>
        public bool ShowProgressBar => MetadataTotalCount > 0;

        /// <summary>
        /// Requests cancel for the in-progress operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// During add, the engine stops resolving sources and discards its staging batch (items not yet in the
        /// rename list), so the live list stays unchanged. Canceling preview also disables Auto-Preview.
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanCancel))]
        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Runs add work on a background thread, reports progress, and shows the dialog only if it takes long enough.
        /// </summary>
        /// <param name="work">Engine work invoked with the operation cancel token and progress sink.</param>
        /// <returns>
        /// <see langword="true"/> when the work finished without user cancel; <see langword="false"/> when canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is <see langword="null"/>.</exception>
        internal Task<bool> RunAsync(Action<CancellationToken, IProgress<RenameListProgress>> work)
        {
            return RunAsync(RenameListProgressOperation.Add, work);
        }

        /// <summary>
        /// Runs background work with operation-specific dialog copy.
        /// </summary>
        /// <param name="operation">Add, metadata hydrate, refresh, or preview.</param>
        /// <param name="work">Engine work invoked with the operation cancel token and progress sink.</param>
        /// <returns>
        /// <see langword="true"/> when the work finished without user cancel; <see langword="false"/> when canceled
        /// or when another operation is already running.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// Refuses to start when another operation is already running so a second caller cannot replace the
        /// in-flight cancel token or run two engine walks at once.
        /// </para>
        /// </remarks>
        internal async Task<bool> RunAsync(
            RenameListProgressOperation operation,
            Action<CancellationToken, IProgress<RenameListProgress>> work
        )
        {
            ArgumentNullException.ThrowIfNull(work);

            if (IsBusy)
            {
                return false;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progress = new Progress<RenameListProgress>(_ApplyProgress);

            Operation = operation;
            Phase = RenameListProgressCopy.For(operation).InitialPhase;
            IsBusy = true;
            IsDialogVisible = false;
            ScannedCount = 0;
            AddedCount = 0;
            MetadataProcessedCount = 0;
            MetadataTotalCount = 0;
            LastPath = string.Empty;

            var showDialogDelay = Task.Delay(DialogDelayMilliseconds, CancellationToken.None);
            var workTask = Task.Run(() => work(token, progress), token);

            var completed = await Task.WhenAny(workTask, showDialogDelay).ConfigureAwait(true);
            if (completed == showDialogDelay && !workTask.IsCompleted)
            {
                IsDialogVisible = true;
            }

            var canceled = false;
            try
            {
                await workTask.ConfigureAwait(true);
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
                // Clear IsBusy before hiding the dialog so programmatic Close is not canceled.
                IsBusy = false;
                IsDialogVisible = false;
                _cts.Dispose();
                _cts = null;
            }

            // Let the progress dialog Close continuation run before callers refresh the grid.
            await Task.Yield();

            return !canceled;
        }

        private void _ApplyProgress(RenameListProgress progress)
        {
            Phase = progress.Phase;
            ScannedCount = progress.ScannedCount;
            AddedCount = progress.AddedCount;
            MetadataProcessedCount = progress.MetadataProcessedCount;
            MetadataTotalCount = progress.MetadataTotalCount;
            LastPath = progress.LastPath;
        }

        private bool _CanCancel()
        {
            return IsBusy;
        }
    }
}
