using Avalonia;
using Avalonia.Threading;
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
    }
}
