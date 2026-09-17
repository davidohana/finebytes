using Mfr.App.Ui.Services;
using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.About;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Ui.About
{
    /// <summary>
    /// Unit tests for About dialog content and Help Index / Tips open wiring.
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

    /// <summary>
    /// Main-window Help Index / Tips open through <see cref="FilterHelpHost"/>.
    /// </summary>
    public sealed class MainWindowHelpCommandTests
    {
        /// <summary>
        /// Verifies ShowHelp opens <c>index.html</c> when present.
        /// </summary>
        [Fact]
        public void ShowHelp_opens_index_html()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-index-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var helpFile = Path.Combine(helpDir, "index.html");
                File.WriteAllText(helpFile, "<html></html>");
                var opener = new RecordingFileShellOpener();
                var host = new FilterHelpHost(opener, [helpDir]);
                var viewModel = new MainWindowViewModel(helpHost: host);
                string? missing = null;
                viewModel.HelpMissing += (_, fileName) => missing = fileName;

                viewModel.ShowHelpCommand.Execute(null);

                Assert.Null(missing);
                Assert.Equal([helpFile], opener.OpenedWithDefaultApp);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies ShowTips opens <c>tips.html</c> when present.
        /// </summary>
        [Fact]
        public void ShowTips_opens_tips_html()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-tips-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var helpFile = Path.Combine(helpDir, "tips.html");
                File.WriteAllText(helpFile, "<html></html>");
                var opener = new RecordingFileShellOpener();
                var host = new FilterHelpHost(opener, [helpDir]);
                var viewModel = new MainWindowViewModel(helpHost: host);
                string? missing = null;
                viewModel.HelpMissing += (_, fileName) => missing = fileName;

                viewModel.ShowTipsCommand.Execute(null);

                Assert.Null(missing);
                Assert.Equal([helpFile], opener.OpenedWithDefaultApp);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies missing Index raises <see cref="MainWindowViewModel.HelpMissing"/>.
        /// </summary>
        [Fact]
        public void ShowHelp_raises_missing_when_index_absent()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-index-missing-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var opener = new RecordingFileShellOpener();
                var host = new FilterHelpHost(opener, [helpDir]);
                var viewModel = new MainWindowViewModel(helpHost: host);
                string? missing = null;
                viewModel.HelpMissing += (_, fileName) => missing = fileName;

                viewModel.ShowHelpCommand.Execute(null);

                Assert.Equal("index.html", missing);
                Assert.Empty(opener.OpenedWithDefaultApp);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }
    }
}
