namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="RenameListProgressTracker"/> progress snapshots.
    /// </summary>
    public sealed class RenameListProgressTrackerTests
    {
        /// <summary>
        /// Verifies metadata reports keep resolve totals and use a separate processed count.
        /// </summary>
        [Fact]
        public void Metadata_Phase_Keeps_Resolve_Counts()
        {
            var reports = new List<RenameListProgress>();
            var tracker = new RenameListProgressTracker(new SynchronousProgress<RenameListProgress>(reports.Add));

            tracker.OnScanned(@"C:\a.mp3");
            tracker.OnAdded(@"C:\a.mp3");
            tracker.BeginMetadataPhase(2);
            tracker.OnMetadataProcessed(@"C:\a.mp3");
            tracker.ReportFinal();

            var last = reports[^1];
            Assert.Equal(1, last.ScannedCount);
            Assert.Equal(1, last.AddedCount);
            Assert.Equal(1, last.MetadataProcessedCount);
            Assert.Equal(2, last.MetadataTotalCount);
            Assert.Equal(RenameListProgressPhase.LoadMetadata, last.Phase);
            Assert.Equal(@"C:\a.mp3", last.LastPath);
        }

        /// <summary>
        /// Verifies cancel is visible as soon as the token is signaled.
        /// </summary>
        [Fact]
        public void IsCanceled_Follows_Token()
        {
            using var cts = new CancellationTokenSource();
            var tracker = new RenameListProgressTracker(progress: null, cts.Token);

            Assert.False(tracker.IsCanceled);
            cts.Cancel();
            Assert.True(tracker.IsCanceled);
        }
    }
}
