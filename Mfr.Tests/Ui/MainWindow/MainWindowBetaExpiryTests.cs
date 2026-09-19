using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Engine.Beta;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window GO / Generate Rename Script gating when beta enforcement is active.
    /// </summary>
    [Collection(BetaExpiryGateCollection.Name)]
    public sealed class MainWindowBetaExpiryTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            BetaExpiryGate.ResetForTests();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies GO and Generate Rename Script stay disabled when the beta clock is past expiry.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_and_GenerateRenameScript_disabled_when_beta_expired()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );

            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "alpha").ConfigureAwait(true);
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            Assert.False(viewModel.GoCommand.CanExecute(null));
            Assert.False(viewModel.GenerateRenameScriptCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the startup probe callback publishes the shared expiry status and refreshes CanExecute.
        /// </summary>
        [AvaloniaFact]
        public async Task NotifyBetaExpiryProbeCompleted_sets_status_when_expired()
        {
            BetaExpiryGate.ConfigureForTests(
                utcNow: () => BetaExpiryGate.ExpiresUtc,
                tryFetchNetworkUtc: static _ => null
            );

            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "alpha").ConfigureAwait(true);
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            viewModel.NotifyBetaExpiryProbeCompleted();

            Assert.Equal(
                BetaExpiredException.FormatMessage(BetaExpiryGate.ExpiresUtc),
                viewModel.StatusHint.ToPlainText()
            );
            Assert.False(viewModel.GoCommand.CanExecute(null));
        }
    }
}
