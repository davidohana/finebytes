using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.MainWindow;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Tools → Generate Rename Script command: format inference, empty gate, and status text.
    /// </summary>
    public sealed class MainWindowGenerateRenameScriptTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies a <c>.bat</c> path writes a batch script and sets a success status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_bat_writes_script_and_sets_success_status()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            viewModel.PickRenameScriptPathAsync = () => Task.FromResult<string?>(outPath);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(outPath));
            var text = await File.ReadAllTextAsync(outPath).ConfigureAwait(true);
            Assert.Contains("ren ", text, StringComparison.Ordinal);
            Assert.Equal(
                "Generated rename script for 1 item(s).",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
            Assert.All(
                viewModel.RenameListViewModel.LastStatusMessage.Runs,
                run => Assert.Null(run.ForegroundResourceKey)
            );
        }

        /// <summary>
        /// Verifies a <c>.ps1</c> path writes PowerShell and sets a success status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_ps1_writes_script_and_sets_success_status()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.ps1").ConfigureAwait(true);
            viewModel.PickRenameScriptPathAsync = () => Task.FromResult<string?>(outPath);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(outPath));
            var text = await File.ReadAllTextAsync(outPath).ConfigureAwait(true);
            Assert.Contains("Rename-Item", text, StringComparison.Ordinal);
            Assert.Equal(
                "Generated rename script for 1 item(s).",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies cancel leaves disk alone and sets a cancel status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_cancel_leaves_disk_alone()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("cancelled.bat").ConfigureAwait(true);
            viewModel.PickRenameScriptPathAsync = () => Task.FromResult<string?>(null);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(outPath));
            Assert.Equal(
                "Generate Rename Script cancelled.",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies an empty Collect gate skips the picker and sets a warning status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_no_scriptable_changes_skips_picker()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "x").ConfigureAwait(true);
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var pickerCalled = false;
            viewModel.PickRenameScriptPathAsync = () =>
            {
                pickerCalled = true;
                return Task.FromResult<string?>(Path.Combine(dir, "out.bat"));
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(pickerCalled);
            Assert.Equal(
                "No scriptable changes to export.",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
            Assert.Equal(
                StatusBarText.WarningForegroundResourceKey,
                viewModel.RenameListViewModel.LastStatusMessage.Runs[0].ForegroundResourceKey
            );
        }

        /// <summary>
        /// Verifies an unsupported extension sets an error status and does not write.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_invalid_extension_sets_error_status()
        {
            var (viewModel, _) = await _PrepareScriptableListAsync("ignored.bat").ConfigureAwait(true);
            var badPath = Path.Combine(_tempDirectoryFixture.CreateTempDir(), "out.txt");
            viewModel.PickRenameScriptPathAsync = () => Task.FromResult<string?>(badPath);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(badPath));
            Assert.Equal("Choose a .bat or .ps1 file.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
            Assert.Equal(
                StatusBarText.ErrorForegroundResourceKey,
                viewModel.RenameListViewModel.LastStatusMessage.Runs[0].ForegroundResourceKey
            );
        }

        /// <summary>
        /// Verifies an IO failure sets an error status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_io_error_sets_error_status()
        {
            var (viewModel, _) = await _PrepareScriptableListAsync("unused.bat").ConfigureAwait(true);
            var missingDir = Path.Combine(_tempDirectoryFixture.CreateTempDir(), "missing", "out.bat");
            viewModel.PickRenameScriptPathAsync = () => Task.FromResult<string?>(missingDir);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(missingDir));
            Assert.StartsWith(
                "Failed to generate rename script:",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText(),
                StringComparison.Ordinal
            );
            Assert.Equal(
                StatusBarText.ErrorForegroundResourceKey,
                viewModel.RenameListViewModel.LastStatusMessage.Runs[0].ForegroundResourceKey
            );
        }

        private async Task<(MainWindowViewModel ViewModel, string OutPath)> _PrepareScriptableListAsync(
            string outFileName
        )
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "x").ConfigureAwait(true);
            var outPath = Path.Combine(dir, outFileName);

            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var item = Assert.Single(viewModel.RenameListViewModel.Entries).EngineItem;
            item.Preview.FileName = "beta";

            return (viewModel, outPath);
        }
    }
}
