using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;
using Mfr.Filters.Case;
using Mfr.Filters.Space;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Utils;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Tests main-window restore of the working Applied Filters chain from session.
    /// </summary>
    public sealed class MainWindowAppliedFiltersSessionTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies launch restore uses <c>ReplaceFromChain</c> (catalog names, enabled flags) and does not
        /// set last-loaded preset.
        /// </summary>
        [AvaloniaFact]
        public void Constructor_restores_applied_filters_from_session_without_last_loaded()
        {
            var letters = new LettersCaseFilter();
            var shrink = new ShrinkSpacesFilter();
            var session = new SessionState
            {
                AppliedFilters = new FilterChain
                {
                    Steps =
                    [
                        new FilterChainStep(Enabled: false, Filter: letters),
                        new FilterChainStep(Enabled: true, Filter: shrink),
                    ],
                },
            };

            var viewModel = new MainWindowViewModel(session: session);

            Assert.Null(viewModel.AppliedFiltersViewModel.LastLoaded);
            Assert.Equal(2, viewModel.AppliedFiltersViewModel.Count);
            Assert.Equal("Letters Case", viewModel.AppliedFiltersViewModel.Steps[0].DisplayName);
            Assert.False(viewModel.AppliedFiltersViewModel.Steps[0].Enabled);
            Assert.Equal("Shrink Spaces", viewModel.AppliedFiltersViewModel.Steps[1].DisplayName);
            Assert.True(viewModel.AppliedFiltersViewModel.Steps[1].Enabled);
            Assert.Same(viewModel.AppliedFiltersViewModel.Steps[0], viewModel.AppliedFiltersViewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies chain changes write through to the live session and flush to disk when a path is set.
        /// </summary>
        [AvaloniaFact]
        public async Task ChainChanged_writes_session_and_debounced_disk_when_path_set()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            var session = new SessionState();
            var viewModel = new MainWindowViewModel(session: session, sessionFilePath: path);

            viewModel.AppliedFiltersViewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            Assert.NotNull(session.AppliedFilters);
            Assert.Single(session.AppliedFilters.Steps);

            await viewModel.WaitForPendingAppliedFiltersSessionSaveAsync().ConfigureAwait(true);

            var loaded = SessionStore.Load(path, SessionJsonOptions.Default);
            Assert.NotNull(loaded.AppliedFilters);
            Assert.Single(loaded.AppliedFilters.Steps);
            Assert.Equal("ShrinkSpaces", loaded.AppliedFilters.Steps[0].Filter.Type);
        }
    }
}
