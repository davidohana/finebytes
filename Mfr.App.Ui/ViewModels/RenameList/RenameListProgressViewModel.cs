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
        private const int DialogDelayMilliseconds = 200;

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
        /// <para>
        /// Stays false until the operation has been running longer than a short delay, so fast work never flashes a dialog.
        /// </para>
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
        private int _metadataTotalCount;

        /// <summary>
        /// Gets the most recent path considered during the current operation.
        /// </summary>
        [ObservableProperty]
        private string _lastPath = string.Empty;

        /// <summary>
        /// Gets the progress dialog window title for the current operation.
        /// </summary>
        public string DialogTitle
        {
            get
            {
                if (Operation == RenameListProgressOperation.Refresh)
                {
                    return "Refreshing Rename List";
                }

                if (Operation == RenameListProgressOperation.Preview)
                {
                    return "Previewing ...";
                }

                if (Phase == RenameListProgressPhase.LoadMetadata)
                {
                    return "Reading file metadata";
                }

                return "Adding to Rename List";
            }
        }

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
        public string MetadataProgressText
        {
            get
            {
                if (Operation == RenameListProgressOperation.Refresh)
                {
                    return $"Refreshing: {MetadataProcessedCount} of {MetadataTotalCount} files";
                }

                if (Operation == RenameListProgressOperation.Preview)
                {
                    return $"Previewing: {MetadataProcessedCount} of {MetadataTotalCount} files";
                }

                return $"Reading metadata: {MetadataProcessedCount} of {MetadataTotalCount} files";
            }
        }

        /// <summary>
        /// Gets whether scanned/added resolve lines should be shown.
        /// </summary>
        public bool ShowResolveProgress => Operation == RenameListProgressOperation.Add;

        /// <summary>
        /// Gets whether the per-row progress line should be shown.
        /// </summary>
        public bool ShowMetadataProgress => Phase == RenameListProgressPhase.LoadMetadata;

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
        /// <see langword="true"/> when the work finished without user cancel; <see langword="false"/> when canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is <see langword="null"/>.</exception>
        internal async Task<bool> RunAsync(
            RenameListProgressOperation operation,
            Action<CancellationToken, IProgress<RenameListProgress>> work
        )
        {
            ArgumentNullException.ThrowIfNull(work);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progress = new Progress<RenameListProgress>(_ApplyProgress);

            Operation = operation;
            Phase = operation
                is RenameListProgressOperation.MetadataHydrate
                    or RenameListProgressOperation.Refresh
                    or RenameListProgressOperation.Preview
                ? RenameListProgressPhase.LoadMetadata
                : RenameListProgressPhase.ResolveSources;
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
