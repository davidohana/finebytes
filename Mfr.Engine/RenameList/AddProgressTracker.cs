namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Progress snapshot while resolving and appending rename sources.
    /// </summary>
    /// <param name="ScannedCount">Filesystem entries visited during resolution.</param>
    /// <param name="AddedCount">Items newly accepted into the rename list for this operation.</param>
    /// <param name="LastPath">Most recent path considered (scanned or added).</param>
    public sealed record RenameListAddProgress(int ScannedCount, int AddedCount, string LastPath);

    /// <summary>
    /// Throttles <see cref="RenameListAddProgress"/> reports and holds the cancel flag for one add operation.
    /// </summary>
    /// <param name="progress">Optional progress sink; when null, counts are still tracked for a final report.</param>
    /// <param name="cancellationToken">When canceled, the add walk should stop without throwing.</param>
    internal sealed class AddProgressTracker(
        IProgress<RenameListAddProgress>? progress,
        CancellationToken cancellationToken = default
    )
    {
        private const int ProgressRefreshMilliseconds = 200;

        private readonly IProgress<RenameListAddProgress>? _progress = progress;
        private long _lastReportTicks = Environment.TickCount64;

        /// <summary>
        /// Gets how many filesystem entries have been visited.
        /// </summary>
        public int ScannedCount { get; private set; }

        /// <summary>
        /// Gets how many items were newly accepted into the rename list.
        /// </summary>
        public int AddedCount { get; private set; }

        /// <summary>
        /// Gets the most recent path considered.
        /// </summary>
        public string LastPath { get; private set; } = string.Empty;

        /// <summary>
        /// Gets whether this add operation should stop.
        /// </summary>
        public bool IsCanceled => Token.IsCancellationRequested;

        /// <summary>
        /// Gets the token to pass to enumerators that accept <see cref="CancellationToken"/>.
        /// </summary>
        public CancellationToken Token { get; } = cancellationToken;

        /// <summary>
        /// Records a scanned filesystem entry and may report progress.
        /// </summary>
        /// <param name="path">Path that was visited.</param>
        public void OnScanned(string path)
        {
            ScannedCount++;
            LastPath = path;
            _MaybeReport();
        }

        /// <summary>
        /// Records a newly added rename-list item and may report progress.
        /// </summary>
        /// <param name="path">Path that was added.</param>
        public void OnAdded(string path)
        {
            AddedCount++;
            LastPath = path;
            _MaybeReport();
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
        private void _MaybeReport()
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
            _progress?.Report(new RenameListAddProgress(ScannedCount, AddedCount, LastPath));
        }
    }
}
