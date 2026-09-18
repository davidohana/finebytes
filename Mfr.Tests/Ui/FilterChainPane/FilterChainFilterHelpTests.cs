using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FilterChainPane;

namespace Mfr.Tests.Ui.FilterChainPane
{
    /// <summary>
    /// Filter Chain help command enablement and open wiring.
    /// </summary>
    public sealed class FilterChainFilterHelpTests
    {
        /// <summary>
        /// Verifies help is enabled for a single mapped selection and opens the file.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_opens_mapped_file()
        {
            TempHelpRoot.Run(
                (helpDir, opener, host) =>
                {
                    var helpFile = Path.Combine(helpDir, "LettersCase.html");
                    var viewModel = new FilterChainViewModel(helpHost: host);
                    viewModel.AddCommand.Execute(FilterChainTestUi.Entry("LettersCase"));

                    Assert.True(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
                    string? missing = null;
                    viewModel.FilterHelpMissing += (_, fileName) => missing = fileName;

                    viewModel.OpenSelectedFilterHelpCommand.Execute(null);

                    Assert.Null(missing);
                    Assert.Equal([helpFile], opener.OpenedWithDefaultApp);
                },
                "LettersCase.html"
            );
        }

        /// <summary>
        /// Verifies help raises <see cref="FilterChainViewModel.FilterHelpMissing"/> when the file is absent.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_raises_missing_when_file_absent()
        {
            TempHelpRoot.Run(
                (_, opener, host) =>
                {
                    var viewModel = new FilterChainViewModel(helpHost: host);
                    viewModel.AddCommand.Execute(FilterChainTestUi.Entry("SpaceCharacter"));
                    string? missing = null;
                    viewModel.FilterHelpMissing += (_, fileName) => missing = fileName;

                    viewModel.OpenSelectedFilterHelpCommand.Execute(null);

                    Assert.Equal("SpaceCharacter.html", missing);
                    Assert.Empty(opener.OpenedWithDefaultApp);
                }
            );
        }

        /// <summary>
        /// Verifies help is disabled for multi-select (same as Filter Options / pin / reset).
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_disabled_for_multi_select()
        {
            var viewModel = new FilterChainViewModel(helpHost: new HelpHost(NullFileShellOpener.Instance, []));
            viewModel.AddCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(FilterChainTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);

            Assert.False(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies help is disabled with an empty Filter Chain.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_disabled_when_empty()
        {
            var viewModel = new FilterChainViewModel(helpHost: new HelpHost(NullFileShellOpener.Instance, []));
            Assert.False(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
        }
    }
}
