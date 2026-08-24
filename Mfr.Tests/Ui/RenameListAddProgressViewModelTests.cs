using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests delayed dialog visibility, progress updates, and cancel for Rename List add.
    /// </summary>
    public sealed class RenameListAddProgressViewModelTests
    {
        /// <summary>
        /// Verifies a fast add completes without showing the progress dialog.
        /// </summary>
        [Fact]
        public async Task RunAsync_Fast_Work_Does_Not_Show_Dialog()
        {
            var viewModel = new RenameListAddProgressViewModel();
            var dialogBecameVisible = false;
            viewModel.PropertyChanged += (_, e) =>
            {
                if (
                    e.PropertyName is nameof(RenameListAddProgressViewModel.IsDialogVisible)
                    && viewModel.IsDialogVisible
                )
                {
                    dialogBecameVisible = true;
                }
            };

            var completed = await viewModel.RunAsync((_, _) => { }).ConfigureAwait(true);

            Assert.True(completed);
            Assert.False(dialogBecameVisible);
            Assert.False(viewModel.IsAdding);
            Assert.False(viewModel.IsDialogVisible);
            Assert.False(viewModel.CancelCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the dialog appears after the delay while a slow add is still running, then hides.
        /// </summary>
        [Fact]
        public async Task RunAsync_Slow_Work_Shows_Dialog_Then_Hides()
        {
            var viewModel = new RenameListAddProgressViewModel();
            var dialogBecameVisible = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            viewModel.PropertyChanged += (_, e) =>
            {
                if (
                    e.PropertyName is nameof(RenameListAddProgressViewModel.IsDialogVisible)
                    && viewModel.IsDialogVisible
                )
                {
                    dialogBecameVisible.TrySetResult();
                }
            };

            var run = viewModel.RunAsync((_, _) => Thread.Sleep(400));
            await dialogBecameVisible.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(true);

            Assert.True(viewModel.IsAdding);
            Assert.True(viewModel.IsDialogVisible);
            Assert.True(viewModel.CancelCommand.CanExecute(null));

            Assert.True(await run.ConfigureAwait(true));
            Assert.False(viewModel.IsAdding);
            Assert.False(viewModel.IsDialogVisible);
        }

        /// <summary>
        /// Verifies cancel stops the worker and reports the add as not completed.
        /// </summary>
        [Fact]
        public async Task RunAsync_Cancel_Returns_False()
        {
            var viewModel = new RenameListAddProgressViewModel();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = viewModel.RunAsync(
                (token, _) =>
                {
                    started.TrySetResult();
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(20);
                    }
                }
            );

            await started.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
            Assert.True(viewModel.IsAdding);
            Assert.True(viewModel.CancelCommand.CanExecute(null));

            viewModel.CancelCommand.Execute(null);

            Assert.False(await run.ConfigureAwait(true));
            Assert.False(viewModel.IsAdding);
            Assert.False(viewModel.IsDialogVisible);
            Assert.False(viewModel.CancelCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies engine progress snapshots are copied onto the view-model properties.
        /// </summary>
        [Fact]
        public async Task RunAsync_Applies_Progress_Reports()
        {
            var viewModel = new RenameListAddProgressViewModel();
            var lastPath = Path.Combine("folder", "file.txt");

            var completed = await viewModel
                .RunAsync(
                    (_, progress) =>
                    {
                        progress.Report(new RenameListAddProgress(4, 2, lastPath));
                        var deadline = Environment.TickCount64 + 2000;
                        while (viewModel.ScannedCount != 4 && Environment.TickCount64 < deadline)
                        {
                            Thread.Sleep(10);
                        }
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal(4, viewModel.ScannedCount);
            Assert.Equal(2, viewModel.AddedCount);
            Assert.Equal(lastPath, viewModel.LastPath);
        }
    }
}
