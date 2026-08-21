using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the unexpected-error dialog.
    /// </summary>
    public sealed class CrashDialogTests
    {
        [Fact]
        /// <summary>
        /// Verifies summary includes the close warning and log fallback text.
        /// </summary>
        public void ViewModel_Sets_Summary_And_Log_Fallback()
        {
            var viewModel = new CrashDialogViewModel(
                details: "details",
                logFilePath: null,
                logDirectoryPath: string.Empty);

            Assert.Contains("terminated", viewModel.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Diagnostic log was not written.", viewModel.LogFileDisplay);
            Assert.False(viewModel.HasLogDirectory);
        }

        [AvaloniaFact]
        /// <summary>
        /// Verifies the crash dialog constructs and shows in the headless lifetime.
        /// </summary>
        public void CrashDialog_Constructs()
        {
            var viewModel = new CrashDialogViewModel(
                details: "System.InvalidOperationException: boom",
                logFilePath: @"C:\logs\session-test.log",
                logDirectoryPath: @"C:\logs");
            var dialog = new CrashDialog(viewModel);
            dialog.Show();

            Assert.True(dialog.IsVisible);
            Assert.Same(viewModel, dialog.DataContext);
            Assert.Contains("terminated", viewModel.Summary, StringComparison.OrdinalIgnoreCase);
        }
    }
}
