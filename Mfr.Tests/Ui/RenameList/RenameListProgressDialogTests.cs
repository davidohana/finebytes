using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless smoke tests for the Rename List progress dialog chrome.
    /// </summary>
    public sealed class RenameListProgressDialogTests
    {
        /// <summary>
        /// Verifies the progress dialog constructs and shows without throwing after Keep-button AXAML.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_without_throwing()
        {
            var viewModel = new RenameListProgressViewModel();
            var dialog = new RenameListProgressDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();

            Assert.Equal("Adding to Rename List", dialog.Title);
            Assert.False(viewModel.ShowKeepAdded);
        }

        /// <summary>
        /// Verifies closing the dialog while busy requests Cancel (discard), not Keep.
        /// </summary>
        [AvaloniaFact]
        public async Task Closing_while_busy_requests_cancel_not_keep()
        {
            var viewModel = new RenameListProgressViewModel();
            var disposition = new RenameListAddCancelDisposition();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = viewModel.RunAsync(
                (token, _) =>
                {
                    started.TrySetResult();
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(20);
                    }
                },
                addCancelDisposition: disposition
            );

            await started.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
            Assert.True(viewModel.IsBusy);
            Assert.True(viewModel.ShowKeepAdded);

            var dialog = new RenameListProgressDialog(viewModel);
            dialog.Show();
            dialog.Close();

            Assert.Equal(RenameListProgressResult.Canceled, await run.ConfigureAwait(true));
            Assert.False(disposition.KeepPartial);
            Assert.False(viewModel.IsBusy);
        }
    }
}
