namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Progress snapshot while resolving sources and hydrating metadata.
    /// </summary>
    /// <param name="ScannedCount">Filesystem entries visited during resolve.</param>
    /// <param name="AddedCount">Items newly accepted into the rename list during resolve.</param>
    /// <param name="LastPath">Most recent path considered.</param>
    /// <param name="MetadataTotalCount">Total rows for metadata hydrate; zero during resolve.</param>
    /// <param name="Phase">Current stage of the operation.</param>
    /// <param name="MetadataProcessedCount">Rows whose metadata has been read during hydrate.</param>
    public sealed record RenameListAddProgress(
        int ScannedCount,
        int AddedCount,
        string LastPath,
        int MetadataTotalCount = 0,
        RenameListAddProgressPhase Phase = RenameListAddProgressPhase.ResolveSources,
        int MetadataProcessedCount = 0
    );

    /// <summary>
    /// Throttles <see cref="RenameListAddProgress"/> reports and holds the cancel flag for one operation.
    /// </summary>
    /// <param name="progress">Optional progress sink; when null, counts are still tracked for a final report.</param>
    /// <param name="cancellationToken">When canceled, the walk should stop without throwing.</param>
    internal sealed class AddProgressTracker(
        IProgress<RenameListAddProgress>? progress,
        CancellationToken cancellationToken = default
    )
    {
        private const int ProgressRefreshMilliseconds = 200;

        private readonly IProgress<RenameListAddProgress>? _progress = progress;
        private long _lastReportTicks = Environment.TickCount64;
        private RenameListAddProgressPhase _phase = RenameListAddProgressPhase.ResolveSources;
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
        /// Switches progress to the metadata hydrate stage.
        /// </summary>
        /// <param name="totalItems">Rows to read metadata for.</param>
        public void BeginMetadataPhase(int totalItems)
        {
            _phase = RenameListAddProgressPhase.LoadMetadata;
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
        /// Records one metadata row processed during the hydrate stage.
        /// </summary>
        /// <param name="path">Path whose metadata was read or skipped.</param>
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
                new RenameListAddProgress(
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
