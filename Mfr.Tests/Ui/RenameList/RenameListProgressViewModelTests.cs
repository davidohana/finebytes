using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests delayed dialog visibility, progress updates, and cancel for Rename List background work.
    /// </summary>
    public sealed class RenameListProgressViewModelTests
    {
        /// <summary>
        /// Verifies a fast add completes without showing the progress dialog.
        /// </summary>
        [Fact]
        public async Task RunAsync_Fast_Work_Does_Not_Show_Dialog()
        {
            var viewModel = new RenameListProgressViewModel();
            var dialogBecameVisible = false;
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(RenameListProgressViewModel.IsDialogVisible) && viewModel.IsDialogVisible)
                {
                    dialogBecameVisible = true;
                }
            };

            var completed = await viewModel.RunAsync((_, _) => { }).ConfigureAwait(true);

            Assert.True(completed);
            Assert.False(dialogBecameVisible);
            Assert.False(viewModel.IsBusy);
            Assert.False(viewModel.IsDialogVisible);
            Assert.False(viewModel.CancelCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the dialog appears after the delay while a slow add is still running, then hides.
        /// </summary>
        [Fact]
        public async Task RunAsync_Slow_Work_Shows_Dialog_Then_Hides()
        {
            var viewModel = new RenameListProgressViewModel();
            var dialogBecameVisible = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(RenameListProgressViewModel.IsDialogVisible) && viewModel.IsDialogVisible)
                {
                    dialogBecameVisible.TrySetResult();
                }
            };

            var run = viewModel.RunAsync((_, _) => Thread.Sleep(400));
            await dialogBecameVisible.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(true);

            Assert.True(viewModel.IsBusy);
            Assert.True(viewModel.IsDialogVisible);
            Assert.True(viewModel.CancelCommand.CanExecute(null));

            Assert.True(await run.ConfigureAwait(true));
            Assert.False(viewModel.IsBusy);
            Assert.False(viewModel.IsDialogVisible);
        }

        /// <summary>
        /// Verifies cancel stops the worker and reports the add as not completed.
        /// </summary>
        [Fact]
        public async Task RunAsync_Cancel_Returns_False()
        {
            var viewModel = new RenameListProgressViewModel();
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
            Assert.True(viewModel.IsBusy);
            Assert.True(viewModel.CancelCommand.CanExecute(null));

            viewModel.CancelCommand.Execute(null);

            Assert.False(await run.ConfigureAwait(true));
            Assert.False(viewModel.IsBusy);
            Assert.False(viewModel.IsDialogVisible);
            Assert.False(viewModel.CancelCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies engine progress snapshots are copied onto the view-model properties.
        /// </summary>
        [Fact]
        public async Task RunAsync_Applies_Progress_Reports()
        {
            var viewModel = new RenameListProgressViewModel();
            var lastPath = Path.Combine("folder", "file.txt");

            var completed = await viewModel
                .RunAsync(
                    (_, progress) =>
                    {
                        progress.Report(new RenameListProgress(4, 2, lastPath));
                        _WaitFor(() => viewModel.ScannedCount == 4);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal(4, viewModel.ScannedCount);
            Assert.Equal(2, viewModel.AddedCount);
            Assert.Equal(lastPath, viewModel.LastPath);
        }

        /// <summary>
        /// Verifies metadata hydrate operations expose total counts and dialog title.
        /// </summary>
        [Fact]
        public async Task RunAsync_MetadataHydrate_Uses_Hydrate_Copy()
        {
            var viewModel = new RenameListProgressViewModel();

            var completed = await viewModel
                .RunAsync(
                    RenameListProgressOperation.MetadataHydrate,
                    (_, progress) =>
                    {
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 0,
                                AddedCount: 0,
                                LastPath: "C:\\a.mp3",
                                MetadataTotalCount: 10,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 3
                            )
                        );
                        _WaitFor(() => viewModel.MetadataProcessedCount == 3);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal("Reading file metadata", viewModel.DialogTitle);
            Assert.Equal("Reading metadata: 3 of 10 files", viewModel.MetadataProgressText);
            Assert.True(viewModel.ShowMetadataProgress);
            Assert.False(viewModel.ShowResolveProgress);
        }

        /// <summary>
        /// Verifies original refresh uses refresh dialog copy even after a prior metadata hydrate.
        /// </summary>
        [Fact]
        public async Task RunAsync_Refresh_Uses_Refresh_Copy()
        {
            var viewModel = new RenameListProgressViewModel();
            _ = await viewModel
                .RunAsync(RenameListProgressOperation.MetadataHydrate, (_, _) => { })
                .ConfigureAwait(true);

            var completed = await viewModel
                .RunAsync(
                    RenameListProgressOperation.Refresh,
                    (_, progress) =>
                    {
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 0,
                                AddedCount: 0,
                                LastPath: "C:\\a.mp3",
                                MetadataTotalCount: 10,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 4
                            )
                        );
                        _WaitFor(() => viewModel.MetadataProcessedCount == 4);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal("Refreshing Rename List", viewModel.DialogTitle);
            Assert.Equal("Refreshing: 4 of 10 files", viewModel.MetadataProgressText);
            Assert.True(viewModel.ShowMetadataProgress);
            Assert.False(viewModel.ShowResolveProgress);
        }

        /// <summary>
        /// Verifies add switches to a metadata stage instead of continuing the scanned counter.
        /// </summary>
        [Fact]
        public async Task RunAsync_Add_Metadata_Phase_Switches_Progress_Copy()
        {
            var viewModel = new RenameListProgressViewModel();

            var completed = await viewModel
                .RunAsync(
                    (_, progress) =>
                    {
                        progress.Report(new RenameListProgress(100, 50, "C:\\done.mp3"));
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 100,
                                AddedCount: 50,
                                LastPath: "C:\\a.mp3",
                                MetadataTotalCount: 50,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 1
                            )
                        );
                        _WaitFor(() => viewModel.MetadataProcessedCount == 1);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal(RenameListProgressPhase.LoadMetadata, viewModel.Phase);
            Assert.Equal("Reading file metadata", viewModel.DialogTitle);
            Assert.Equal("Scanned 100 files", viewModel.PrimaryProgressText);
            Assert.Equal("Reading metadata: 1 of 50 files", viewModel.MetadataProgressText);
            Assert.Equal(50, viewModel.AddedCount);
            Assert.True(viewModel.ShowResolveProgress);
            Assert.True(viewModel.ShowMetadataProgress);
            Assert.Equal("Added 50 files", viewModel.SecondaryProgressText);
        }

        /// <summary>
        /// Verifies resolve totals on the first metadata report apply when resolve finished without UI updates.
        /// </summary>
        [Fact]
        public async Task RunAsync_Add_Metadata_Phase_Applies_Resolve_Totals_Without_Prior_Resolve_Report()
        {
            var viewModel = new RenameListProgressViewModel();

            var completed = await viewModel
                .RunAsync(
                    (_, progress) =>
                    {
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 100,
                                AddedCount: 50,
                                LastPath: "C:\\a.mp3",
                                MetadataTotalCount: 50,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 1
                            )
                        );
                        _WaitFor(() => viewModel.MetadataProcessedCount == 1);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal(RenameListProgressPhase.LoadMetadata, viewModel.Phase);
            Assert.Equal("Scanned 100 files", viewModel.PrimaryProgressText);
            Assert.Equal("Added 50 files", viewModel.SecondaryProgressText);
            Assert.Equal("Reading metadata: 1 of 50 files", viewModel.MetadataProgressText);
        }

        /// <summary>
        /// Verifies metadata processed count updates refresh the metadata progress line.
        /// </summary>
        [Fact]
        public async Task RunAsync_Metadata_Progress_Updates_MetadataProgressText()
        {
            var viewModel = new RenameListProgressViewModel();

            var completed = await viewModel
                .RunAsync(
                    RenameListProgressOperation.MetadataHydrate,
                    (_, progress) =>
                    {
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 0,
                                AddedCount: 0,
                                LastPath: "C:\\a.mp3",
                                MetadataTotalCount: 10,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 1
                            )
                        );
                        progress.Report(
                            new RenameListProgress(
                                ScannedCount: 0,
                                AddedCount: 0,
                                LastPath: "C:\\e.mp3",
                                MetadataTotalCount: 10,
                                Phase: RenameListProgressPhase.LoadMetadata,
                                MetadataProcessedCount: 5
                            )
                        );
                        _WaitFor(() => viewModel.MetadataProcessedCount == 5);
                    }
                )
                .ConfigureAwait(true);

            Assert.True(completed);
            Assert.Equal("Reading metadata: 5 of 10 files", viewModel.MetadataProgressText);
        }

        private static void _WaitFor(Func<bool> condition)
        {
            var deadline = Environment.TickCount64 + 2000;
            while (!condition() && Environment.TickCount64 < deadline)
            {
                Thread.Sleep(10);
            }
        }
    }
}
