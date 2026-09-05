namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Progress snapshot for a Rename List background operation.
    /// </summary>
    /// <param name="ScannedCount">Filesystem entries visited during resolve.</param>
    /// <param name="AddedCount">Items newly accepted into the rename list during resolve.</param>
    /// <param name="LastPath">Most recent path considered.</param>
    /// <param name="MetadataTotalCount">Total rows for metadata/preview work; zero during resolve.</param>
    /// <param name="Phase">Current stage of the operation.</param>
    /// <param name="MetadataProcessedCount">Rows processed during metadata hydrate or preview.</param>
    public sealed record RenameListProgress(
        int ScannedCount,
        int AddedCount,
        string LastPath,
        int MetadataTotalCount = 0,
        RenameListProgressPhase Phase = RenameListProgressPhase.ResolveSources,
        int MetadataProcessedCount = 0
    );

    /// <summary>
    /// Throttles <see cref="RenameListProgress"/> reports and holds the cancel flag for one operation.
    /// </summary>
    /// <param name="progress">Optional progress sink; when null, counts are still tracked for a final report.</param>
    /// <param name="cancellationToken">When canceled, the walk should stop without throwing.</param>
    internal sealed class RenameListProgressTracker(
        IProgress<RenameListProgress>? progress,
        CancellationToken cancellationToken = default
    )
    {
        private const int ProgressRefreshMilliseconds = 200;

        private readonly IProgress<RenameListProgress>? _progress = progress;
        // 0 so the first OnScanned/OnAdded reports immediately (same as BeginMetadataPhase).
        private long _lastReportTicks;
        private RenameListProgressPhase _phase = RenameListProgressPhase.ResolveSources;
        private int _metadataProcessedCount;
        private int _metadataTotalCount;

        /// <summary>
        /// Gets how many filesystem entries have been visited during resolve.
        /// </summary>
        public int ScannedCount { get; private set; }

        /// <summary>
        /// Gets how many items were newly accepted into the rename list during resolve.
        /// </summary>
        public int AddedCount { get; private set; }

        /// <summary>
        /// Gets the most recent path considered.
        /// </summary>
        public string LastPath { get; private set; } = string.Empty;

        /// <summary>
        /// Gets whether this operation should stop.
        /// </summary>
        public bool IsCanceled => Token.IsCancellationRequested;

        /// <summary>
        /// Gets the token to pass to enumerators that accept <see cref="CancellationToken"/>.
        /// </summary>
        public CancellationToken Token { get; } = cancellationToken;

        /// <summary>
        /// Switches progress to the per-row work stage (metadata hydrate or preview).
        /// </summary>
        /// <param name="totalItems">Rows to process.</param>
        public void BeginMetadataPhase(int totalItems)
        {
            _phase = RenameListProgressPhase.LoadMetadata;
            _metadataProcessedCount = 0;
            _metadataTotalCount = totalItems;
            _lastReportTicks = 0;
            _Report();
        }

        /// <summary>
        /// Records a scanned filesystem entry and may report progress.
        /// </summary>
        /// <param name="path">Path that was visited.</param>
        public void OnScanned(string path)
        {
            ScannedCount++;
            LastPath = path;
            _ThrottledReport();
        }

        /// <summary>
        /// Records a newly added rename-list item and may report progress.
        /// </summary>
        /// <param name="path">Path that was added.</param>
        public void OnAdded(string path)
        {
            AddedCount++;
            LastPath = path;
            _ThrottledReport();
        }

        /// <summary>
        /// Records one row processed during metadata hydrate or preview.
        /// </summary>
        /// <param name="path">Path whose row was processed or skipped.</param>
        public void OnMetadataProcessed(string path)
        {
            _metadataProcessedCount++;
            LastPath = path;
            _ThrottledReport();
        }

        /// <summary>
        /// Forces a final progress report with current counts.
        /// </summary>
        public void ReportFinal()
        {
            _Report();
        }

        /// <summary>
        /// Reports progress at most once every <see cref="ProgressRefreshMilliseconds"/> so per-file
        /// walk callbacks do not flood the progress sink.
        /// </summary>
        private void _ThrottledReport()
        {
            if (_progress is null)
            {
                return;
            }

            var now = Environment.TickCount64;
            if (now - _lastReportTicks < ProgressRefreshMilliseconds)
            {
                return;
            }

            _lastReportTicks = now;
            _Report();
        }

        private void _Report()
        {
            _progress?.Report(
                new RenameListProgress(
                    ScannedCount,
                    AddedCount,
                    LastPath,
                    _metadataTotalCount,
                    _phase,
                    _metadataProcessedCount
                )
            );
        }
    }
}
