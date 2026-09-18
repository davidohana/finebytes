using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Attributes;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Tools → Generate Rename Script command: format inference, empty gate, and status text.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class MainWindowGenerateRenameScriptTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Initializes a fresh empty config so confirmation-level tests are isolated.
        /// </summary>
        public MainWindowGenerateRenameScriptTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies Generate Rename Script is enabled only for a non-empty idle Rename List.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScriptCommand_requires_nonempty_idle_rename_list()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "x").ConfigureAwait(true);
            var viewModel = new MainWindowViewModel(dir, shellOpener: NullFileShellOpener.Instance);
            viewModel.RenameListViewModel.DisableAutoPreview();

            Assert.False(viewModel.GenerateRenameScriptCommand.CanExecute(null));

            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            Assert.True(viewModel.GenerateRenameScriptCommand.CanExecute(null));

            viewModel.RenameListViewModel.ClearWithoutConfirm();
            Assert.False(viewModel.GenerateRenameScriptCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies a <c>.bat</c> path writes a batch script and sets a success status.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_bat_writes_script_and_sets_success_status()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            _HookSavePath(viewModel, outPath);

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
            _HookSavePath(viewModel, outPath);

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
            _HookSavePath(viewModel, path: null);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(outPath));
            Assert.Equal(
                "Generate Rename Script cancelled.",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies an empty Collect gate skips the picker, sets a warning status, and shows the empty dialog.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_no_scriptable_changes_skips_picker()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "x").ConfigureAwait(true);
            var viewModel = new MainWindowViewModel(dir, shellOpener: NullFileShellOpener.Instance);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var pickerCalled = false;
            var emptyDialogShown = false;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                PickSavePathAsync = _ =>
                {
                    pickerCalled = true;
                    return Task.FromResult<string?>(Path.Combine(dir, "out.bat"));
                },
            };
            viewModel.ShowRenameScriptEmptyAsync = () =>
            {
                emptyDialogShown = true;
                return Task.CompletedTask;
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(pickerCalled);
            Assert.True(emptyDialogShown);
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
        /// Verifies script export asks for bat/ps1 filters via the shared save options.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_uses_multi_type_save_options()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            SaveFilePickOptions? seen = null;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                PickSavePathAsync = options =>
                {
                    seen = options;
                    return Task.FromResult<string?>(outPath);
                },
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.NotNull(seen);
            Assert.Equal("Generate Rename Script", seen.Title);
            Assert.Equal("bat", seen.DefaultExtension);
            Assert.Equal("rename", seen.SuggestedFileName);
            Assert.Equal(3, seen.FileTypes!.Count);
            Assert.Equal("Batch files", seen.FileTypes[0].Name);
            Assert.Equal(["*.bat"], seen.FileTypes[0].Patterns);
            Assert.Equal("PowerShell scripts", seen.FileTypes[1].Name);
            Assert.Equal(["*.ps1"], seen.FileTypes[1].Patterns);
        }

        /// <summary>
        /// Verifies an unsupported extension sets an error status and does not write.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_invalid_extension_sets_error_status()
        {
            var (viewModel, _) = await _PrepareScriptableListAsync("ignored.bat").ConfigureAwait(true);
            var badPath = Path.Combine(_tempDirectoryFixture.CreateTempDir(), "out.txt");
            _HookSavePath(viewModel, badPath);

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
            _HookSavePath(viewModel, missingDir);

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

        /// <summary>
        /// Verifies declining the unsupported-filter warning cancels without writing.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_unsupported_filters_cancel_skips_write()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            viewModel.FilterChainViewModel.AddAndSelect(new DateTimeSetterFilter(), "Date/Time Setter");
            _HookSavePath(viewModel, outPath);

            IReadOnlyList<string>? seenNames = null;
            viewModel.ConfirmRenameScriptUnsupportedFiltersAsync = names =>
            {
                seenNames = names;
                return Task.FromResult(false);
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.Equal(["Date/Time Setter"], seenNames);
            Assert.False(File.Exists(outPath));
            Assert.Equal(
                "Generate Rename Script cancelled.",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies accepting the unsupported-filter warning still writes the script.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_unsupported_filters_accept_writes()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            viewModel.FilterChainViewModel.AddAndSelect(new DateTimeSetterFilter(), "Date/Time Setter");
            _HookSavePath(viewModel, outPath);
            viewModel.ConfirmRenameScriptUnsupportedFiltersAsync = _ => Task.FromResult(true);

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(outPath));
            Assert.Equal(
                "Generated rename script for 1 item(s).",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies a suppressed unsupported-filter confirmation skips the dialog hook.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_unsupported_filters_suppressed_skips_confirm()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.GenerateRenameScriptUnsupportedFilters);

            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            viewModel.FilterChainViewModel.AddAndSelect(new DateTimeSetterFilter(), "Date/Time Setter");
            _HookSavePath(viewModel, outPath);

            var confirmCalled = false;
            viewModel.ConfirmRenameScriptUnsupportedFiltersAsync = _ =>
            {
                confirmCalled = true;
                return Task.FromResult(false);
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(confirmCalled);
            Assert.True(File.Exists(outPath));
        }

        /// <summary>
        /// Verifies disabled unsupported filters do not prompt.
        /// </summary>
        [AvaloniaFact]
        public async Task GenerateRenameScript_disabled_unsupported_filter_skips_confirm()
        {
            var (viewModel, outPath) = await _PrepareScriptableListAsync("out.bat").ConfigureAwait(true);
            viewModel.FilterChainViewModel.AddAndSelect(new DateTimeSetterFilter(), "Date/Time Setter");
            Assert.Single(viewModel.FilterChainViewModel.Steps).Enabled = false;
            _HookSavePath(viewModel, outPath);

            var confirmCalled = false;
            viewModel.ConfirmRenameScriptUnsupportedFiltersAsync = _ =>
            {
                confirmCalled = true;
                return Task.FromResult(false);
            };

            await viewModel.GenerateRenameScriptCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(confirmCalled);
            Assert.True(File.Exists(outPath));
        }

        private static void _HookSavePath(MainWindowViewModel viewModel, string? path)
        {
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                PickSavePathAsync = _ => Task.FromResult(path),
            };
        }

        private async Task<(MainWindowViewModel ViewModel, string OutPath)> _PrepareScriptableListAsync(
            string outFileName
        )
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "x").ConfigureAwait(true);
            var outPath = Path.Combine(dir, outFileName);

            var viewModel = new MainWindowViewModel(dir, shellOpener: NullFileShellOpener.Instance);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var item = Assert.Single(viewModel.RenameListViewModel.Entries).EngineItem;
            item.Preview.FileName = "beta";

            return (viewModel, outPath);
        }
    }
}
