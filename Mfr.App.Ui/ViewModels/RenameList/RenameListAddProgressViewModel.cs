using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Tracks a background Rename List add or metadata hydrate: progress counts, delayed dialog visibility, and cancel.
    /// </summary>
    public sealed partial class RenameListAddProgressViewModel : ViewModelBase
    {
        private const int DialogDelayMilliseconds = 200;

        private CancellationTokenSource? _cts;

        /// <summary>
        /// Gets whether a background operation is running.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        private bool _isAdding;

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
        private RenameListProgressOperation _operation = RenameListProgressOperation.Add;

        /// <summary>
        /// Gets the current stage reported by the engine.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DialogTitle))]
        [NotifyPropertyChangedFor(nameof(ShowMetadataProgress))]
        private RenameListAddProgressPhase _phase = RenameListAddProgressPhase.ResolveSources;

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
        /// Gets how many rows have had metadata read during hydrate.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MetadataProgressText))]
        private int _metadataProcessedCount;

        /// <summary>
        /// Gets the total row count for metadata hydrate; zero during resolve.
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
        public string DialogTitle =>
            Operation switch
            {
                RenameListProgressOperation.Refresh => "Refreshing Rename List",
                RenameListProgressOperation.MetadataHydrate => "Reading file metadata",
                RenameListProgressOperation.Add => "Adding to Rename List",
                _ => "Adding to Rename List",
            };

        /// <summary>
        /// Gets the resolve-stage scan line for add operations.
        /// </summary>
        public string PrimaryProgressText => $"Scanned {ScannedCount} files";

        /// <summary>
        /// Gets the resolve-stage add line for add operations.
        /// </summary>
        public string SecondaryProgressText => $"Added {AddedCount} files";

        /// <summary>
        /// Gets the metadata hydrate line shown as its own row.
        /// </summary>
        public string MetadataProgressText =>
            $"Reading metadata: {MetadataProcessedCount} of {MetadataTotalCount} files";

        /// <summary>
        /// Gets whether scanned/added resolve lines should be shown.
        /// </summary>
        public bool ShowResolveProgress => Operation == RenameListProgressOperation.Add;

        /// <summary>
        /// Gets whether the metadata progress line should be shown.
        /// </summary>
        public bool ShowMetadataProgress => Phase == RenameListAddProgressPhase.LoadMetadata;

        /// <summary>
        /// Requests cancel for the in-progress operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// During add, the engine stops resolving sources and discards its staging batch (items not yet in the
        /// rename list), so the live list stays unchanged.
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
        internal Task<bool> RunAsync(Action<CancellationToken, IProgress<RenameListAddProgress>> work)
        {
            return RunAsync(RenameListProgressOperation.Add, work);
        }

        /// <summary>
        /// Runs background work with operation-specific dialog copy.
        /// </summary>
        /// <param name="operation">Add or metadata hydrate.</param>
        /// <param name="work">Engine work invoked with the operation cancel token and progress sink.</param>
        /// <returns>
        /// <see langword="true"/> when the work finished without user cancel; <see langword="false"/> when canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is <see langword="null"/>.</exception>
        internal async Task<bool> RunAsync(
            RenameListProgressOperation operation,
            Action<CancellationToken, IProgress<RenameListAddProgress>> work
        )
        {
            ArgumentNullException.ThrowIfNull(work);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progress = new Progress<RenameListAddProgress>(_ApplyProgress);

            Operation = operation;
            Phase = operation is RenameListProgressOperation.MetadataHydrate or RenameListProgressOperation.Refresh
                ? RenameListAddProgressPhase.LoadMetadata
                : RenameListAddProgressPhase.ResolveSources;
            IsAdding = true;
            IsDialogVisible = false;
            ScannedCount = 0;
            AddedCount = 0;
            MetadataProcessedCount = 0;
            MetadataTotalCount = 0;
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
            Phase = progress.Phase;
            ScannedCount = progress.ScannedCount;
            AddedCount = progress.AddedCount;
            MetadataProcessedCount = progress.MetadataProcessedCount;
            MetadataTotalCount = progress.MetadataTotalCount;
            LastPath = progress.LastPath;
        }

        private bool _CanCancel()
        {
            return IsAdding;
        }
    }
}
