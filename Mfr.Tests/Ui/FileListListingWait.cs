using Avalonia;
using Avalonia.Threading;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Shared wait helpers for File List async listing in unit and headless tests.
    /// </summary>
    internal static class FileListListingWait
    {
        /// <summary>
        /// Spins until <see cref="FileListViewModel.IsListing"/> is false, pumping the Avalonia dispatcher when present.
        /// </summary>
        /// <param name="viewModel">File List under test.</param>
        public static void WaitUntilIdle(FileListViewModel viewModel)
        {
            var deadline = Environment.TickCount64 + 10_000;
            while (viewModel.IsListing && Environment.TickCount64 < deadline)
            {
                PumpUiDispatcher();
                Thread.Sleep(10);
            }

            Assert.False(viewModel.IsListing);
        }

        /// <summary>
        /// Runs queued Avalonia dispatcher jobs from the UI thread, or marshals a flush when on a worker thread.
        /// </summary>
        public static void PumpUiDispatcher()
        {
            if (Application.Current is null)
            {
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.RunJobs();
                return;
            }

            Dispatcher.UIThread.Invoke(() => Dispatcher.UIThread.RunJobs());
        }

        /// <summary>
        /// Creates a File List whose first catalog call blocks until <see cref="GatedListing.Release"/> is set.
        /// </summary>
        /// <param name="initialPath">Directory to open.</param>
        /// <param name="gate">Started/release events and optional call counter for the gated list.</param>
        /// <returns>A view model that is still listing until the gate is released.</returns>
        public static FileListViewModel CreateWithGatedList(string initialPath, out GatedListing gate)
        {
            gate = new GatedListing();
            var captured = gate;
            return new FileListViewModel(
                NullSystemIconProvider.Instance,
                initialPath,
                NullFileShellOpener.Instance,
                clipboard: NullTextClipboard.Instance,
                shellOperations: NullFileShellOperations.Instance,
                fileClipboard: new NullFileClipboard(),
                ownerHwnd: null,
                listEntries: captured.List
            );
        }
    }

    /// <summary>
    /// Catalog list that signals start and waits for release before returning real folder contents.
    /// </summary>
    internal sealed class GatedListing
    {
        /// <summary>
        /// Set when the catalog list begins (first call).
        /// </summary>
        public ManualResetEventSlim Started { get; } = new();

        /// <summary>
        /// Caller sets this to let the gated list finish.
        /// </summary>
        public ManualResetEventSlim Release { get; } = new();

        /// <summary>
        /// Number of times <see cref="List"/> has been entered.
        /// </summary>
        public int CallCount => _callCount;

        private int _callCount;

        /// <summary>
        /// Blocks on first entry until <see cref="Release"/>, then delegates to <see cref="FileListCatalog.List"/>.
        /// Subsequent calls list immediately (for locate sync reload / refresh after release).
        /// </summary>
        public FileListCatalogResult List(
            string currentPath,
            string includeMask,
            bool excludeMasksEnabled,
            IReadOnlyList<string> excludeMasks,
            IEnumerable<string> pathHistory
        )
        {
            var call = Interlocked.Increment(ref _callCount);
            if (call == 1)
            {
                Started.Set();
                Assert.True(Release.Wait(TimeSpan.FromSeconds(10)));
            }

            return FileListCatalog.List(currentPath, includeMask, excludeMasksEnabled, excludeMasks, pathHistory);
        }
    }
}
