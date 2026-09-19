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
    }
}
