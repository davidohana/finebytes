using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window Help Index / Tips open through <see cref="HelpHost"/>.
    /// </summary>
    public sealed class MainWindowHelpCommandTests
    {
        /// <summary>
        /// Verifies ShowHelp and ShowTips open the expected Help HTML when present.
        /// </summary>
        /// <param name="helpFileName">Basename under the test help root.</param>
        [Theory]
        [InlineData("index.html")]
        [InlineData("tips.html")]
        public void Help_commands_open_expected_html(string helpFileName)
        {
            _WithTempHelpRoot(
                helpFileName,
                writeFile: true,
                (viewModel, opener, helpFile) =>
                {
                    string? missing = null;
                    viewModel.HelpMissing += (_, fileName) => missing = fileName;

                    _InvokeHelpCommand(viewModel, helpFileName);

                    Assert.Null(missing);
                    Assert.Equal([helpFile], opener.OpenedWithDefaultApp);
                }
            );
        }

        /// <summary>
        /// Verifies missing Index raises <see cref="MainWindowViewModel.HelpMissing"/>.
        /// </summary>
        [Fact]
        public void ShowHelp_raises_missing_when_index_absent()
        {
            _WithTempHelpRoot(
                "index.html",
                writeFile: false,
                (viewModel, opener, _) =>
                {
                    string? missing = null;
                    viewModel.HelpMissing += (_, fileName) => missing = fileName;

                    viewModel.ShowHelpCommand.Execute(null);

                    Assert.Equal("index.html", missing);
                    Assert.Empty(opener.OpenedWithDefaultApp);
                }
            );
        }

        /// <summary>
        /// Runs <paramref name="assert"/> with a MainWindow wired to a temp help root.
        /// </summary>
        /// <param name="helpFileName">Basename under the temp root.</param>
        /// <param name="writeFile">When true, writes a minimal HTML file for that basename.</param>
        /// <param name="assert">Assertions against the view model and shell opener.</param>
        private static void _WithTempHelpRoot(
            string helpFileName,
            bool writeFile,
            Action<MainWindowViewModel, RecordingFileShellOpener, string> assert
        )
        {
            var files = writeFile ? new[] { helpFileName } : [];
            TempHelpRoot.Run(
                (helpDir, opener, host) =>
                {
                    var viewModel = new MainWindowViewModel(helpHost: host);
                    assert(viewModel, opener, Path.Combine(helpDir, helpFileName));
                },
                files
            );
        }

        /// <summary>
        /// Invokes the MainWindow command that opens <paramref name="helpFileName"/>.
        /// </summary>
        /// <param name="viewModel">Main window under test.</param>
        /// <param name="helpFileName">Expected Help basename.</param>
        private static void _InvokeHelpCommand(MainWindowViewModel viewModel, string helpFileName)
        {
            switch (helpFileName)
            {
                case "index.html":
                    viewModel.ShowHelpCommand.Execute(null);
                    return;
                case "tips.html":
                    viewModel.ShowTipsCommand.Execute(null);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(helpFileName), helpFileName, null);
            }
        }
    }
}
