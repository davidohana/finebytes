using Mfr.App.Ui.Services;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.About;

namespace Mfr.Tests.Ui.About
{
    /// <summary>
    /// Unit tests for About dialog content and support links.
    /// </summary>
    public sealed class AboutDialogViewModelTests
    {
        /// <summary>
        /// Verifies About uses the same product version and copyright as the shared helper.
        /// </summary>
        [Fact]
        public void About_exposes_shared_version_and_copyright()
        {
            var viewModel = new AboutDialogViewModel(NullFileShellOpener.Instance);

            Assert.Equal(AppProductInfo.GetProductName(), viewModel.ProductName);
            Assert.Equal(AppProductInfo.GetDisplayVersion(), viewModel.DisplayVersion);
            Assert.Equal(AppProductInfo.GetCopyright(), viewModel.Copyright);
            Assert.False(string.IsNullOrWhiteSpace(viewModel.DisplayVersion));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.Copyright));
        }

        /// <summary>
        /// Verifies Web Site and Support e-mail open through the shell opener.
        /// </summary>
        [Fact]
        public void About_opens_site_and_support_via_shell()
        {
            var opener = new RecordingFileShellOpener();
            var viewModel = new AboutDialogViewModel(opener);

            viewModel.OpenWebSiteCommand.Execute(null);
            viewModel.OpenSupportEmailCommand.Execute(null);

            Assert.Equal([AppProductInfo.WebSiteUrl, AppProductInfo.SupportEmailUrl], opener.OpenedWithDefaultApp);
        }
    }
}
