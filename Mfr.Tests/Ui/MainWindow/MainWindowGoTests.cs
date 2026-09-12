using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Replace;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window GO command state and Rename List commit orchestration.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class MainWindowGoTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Initializes a fresh empty config so confirmation-level tests are isolated.
        /// </summary>
        public MainWindowGoTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies GO is enabled only for a non-empty Rename List that is not busy.
        /// </summary>
        [AvaloniaFact]
        public async Task GoCommand_requires_nonempty_idle_rename_list()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(path, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();

            Assert.False(viewModel.GoCommand.CanExecute(null));

            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            Assert.True(viewModel.GoCommand.CanExecute(null));

            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var progressRun = viewModel.RenameListViewModel.Progress.RunAsync(
                RenameListProgressOperation.Preview,
                (_, _) =>
                {
                    started.SetResult();
                    release.Task.GetAwaiter().GetResult();
                }
            );
            await started.Task.ConfigureAwait(true);

            try
            {
                Assert.False(viewModel.GoCommand.CanExecute(null));
            }
            finally
            {
                release.SetResult();
                await progressRun.ConfigureAwait(true);
            }

            Assert.True(viewModel.GoCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the bound main-window GO command previews the live filters, performs the filesystem
        /// rename, and reloads the File List so the new name is visible.
        /// </summary>
        [AvaloniaFact]
        public async Task GoCommand_previews_and_commits_live_filter_chain()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var destination = Path.Combine(dir, "renamed.txt");
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);
            viewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Replacer"));
            viewModel.AppliedFiltersViewModel.Steps[0].SetFilter(_PrefixReplacer("alpha", "renamed"));

            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "alpha.txt");

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(source));
            Assert.True(File.Exists(destination));
            Assert.Equal(RenameListProgressOperation.Commit, viewModel.RenameListViewModel.Progress.Operation);
            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");
        }

        /// <summary>
        /// Verifies declining the preview-error warning aborts before any valid rename is committed
        /// and does not wipe a prior sticky status.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_preview_error_warning_cancel_aborts_commit()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "blocked.txt");
            var occupied = Path.Combine(dir, "taken.txt");
            await File.WriteAllTextAsync(source, "source");
            await File.WriteAllTextAsync(occupied, "occupied");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);

            viewModel.RenameListViewModel.LastStatusMessage = StatusBarText.Neutral("Prior sticky.");

            var warnedCount = 0;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmPreviewErrorsAsync = errorCount =>
                {
                    warnedCount = errorCount;
                    return Task.FromResult(false);
                },
            };

            var commitStarted = await viewModel
                .RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("blocked", "taken")))
                .ConfigureAwait(true);

            Assert.False(commitStarted);
            Assert.Equal(1, warnedCount);
            Assert.True(File.Exists(source));
            Assert.Equal("occupied", await File.ReadAllTextAsync(occupied));
            Assert.Equal(RenameListProgressOperation.Preview, viewModel.RenameListViewModel.Progress.Operation);
            Assert.Equal("Prior sticky.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
            Assert.Equal("Prior sticky.", viewModel.StatusHint.ToPlainText());
        }

        /// <summary>
        /// Verifies Fewer skips the preview-error dialog and still reaches the commit stage.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_preview_errors_fewer_skips_confirm_and_commits()
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            var dir = _tempDirectoryFixture.CreateTempDir();
            var okSource = Path.Combine(dir, "alpha.txt");
            var blocked = Path.Combine(dir, "blocked.txt");
            var occupied = Path.Combine(dir, "taken.txt");
            await File.WriteAllTextAsync(okSource, "alpha");
            await File.WriteAllTextAsync(blocked, "blocked");
            await File.WriteAllTextAsync(occupied, "occupied");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([okSource, blocked]).ConfigureAwait(true);

            var warnedCount = 0;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmPreviewErrorsAsync = errorCount =>
                {
                    warnedCount = errorCount;
                    return Task.FromResult(false);
                },
            };

            var commitStarted = await viewModel
                .RenameListViewModel.GoAsync(
                    _Chain(_PrefixReplacer("alpha", "renamed"), _PrefixReplacer("blocked", "taken"))
                )
                .ConfigureAwait(true);

            Assert.True(commitStarted);
            Assert.Equal(0, warnedCount);
            Assert.True(File.Exists(Path.Combine(dir, "renamed.txt")));
            Assert.True(File.Exists(blocked));
            Assert.Equal("occupied", await File.ReadAllTextAsync(occupied));
            Assert.Equal("Renamed 1 item(s).", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
        }

        /// <summary>
        /// Verifies commit failures set a status-bar GO outcome after valid rows have been attempted.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_commit_errors_set_status()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var blocked = Path.Combine(dir, "blocked.txt");
            var occupied = Path.Combine(dir, "taken.txt");
            await File.WriteAllTextAsync(source, "alpha");
            await File.WriteAllTextAsync(blocked, "blocked");
            await File.WriteAllTextAsync(occupied, "occupied");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source, blocked]).ConfigureAwait(true);

            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmPreviewErrorsAsync = _ =>
                {
                    File.Delete(source);
                    return Task.FromResult(true);
                },
            };

            var commitStarted = await viewModel
                .RenameListViewModel.GoAsync(
                    _Chain(_PrefixReplacer("alpha", "renamed"), _PrefixReplacer("blocked", "taken"))
                )
                .ConfigureAwait(true);

            Assert.True(commitStarted);
            var status = viewModel.RenameListViewModel.LastStatusMessage;
            Assert.Contains("could not be renamed", status.ToPlainText());
            Assert.Contains("Show Rename Error", status.ToPlainText());
            Assert.Contains(
                status.Runs,
                run =>
                    run.Text == "Show Rename Error"
                    && run.FontWeight == FontWeight.Bold
                    && run.ForegroundResourceKey == StatusBarText.ErrorForegroundResourceKey
            );
            Assert.Equal(status.ToPlainText(), viewModel.StatusHint.ToPlainText());
            Assert.Single(viewModel.RenameListViewModel.Entries, entry => entry.HasCommitError);
            Assert.Equal("occupied", await File.ReadAllTextAsync(occupied));
        }

        /// <summary>
        /// Verifies a successful GO reports renamed count in the status-bar outcome.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_success_sets_status()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var destination = Path.Combine(dir, "renamed.txt");
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);

            var commitStarted = await viewModel
                .RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("alpha", "renamed")))
                .ConfigureAwait(true);

            Assert.True(commitStarted);
            Assert.Equal("Renamed 1 item(s).", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
            Assert.Equal("Renamed 1 item(s).", viewModel.StatusHint.ToPlainText());
            Assert.All(
                viewModel.RenameListViewModel.LastStatusMessage.Runs,
                run => Assert.Null(run.ForegroundResourceKey)
            );
            Assert.True(File.Exists(destination));
        }

        /// <summary>
        /// Verifies GO status is visible after an earlier cell hint, and clearing the cell hint
        /// does not restore a previous operation message (last write wins).
        /// </summary>
        [AvaloniaFact]
        public async Task Go_status_last_write_wins_over_cell_hint()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);

            viewModel.RenameListViewModel.CellStatusHint = StyledTextDisplay.FromPlain("Full File Name: alpha.txt");

            await viewModel
                .RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("alpha", "renamed")))
                .ConfigureAwait(true);

            Assert.Equal("Renamed 1 item(s).", viewModel.StatusHint.ToPlainText());

            viewModel.RenameListViewModel.CellStatusHint = StyledTextDisplay.FromPlain("Full File Name: renamed.txt");
            Assert.Equal("Full File Name: renamed.txt", viewModel.StatusHint.ToPlainText());

            viewModel.RenameListViewModel.CellStatusHint = StyledTextDisplay.Empty;
            Assert.True(viewModel.StatusHint.IsEmpty);
        }

        /// <summary>
        /// Verifies clearing the Rename List drops a prior GO status message.
        /// </summary>
        [AvaloniaFact]
        public async Task Clear_after_go_clears_status_hint()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);

            await viewModel
                .RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("alpha", "renamed")))
                .ConfigureAwait(true);

            Assert.Equal("Renamed 1 item(s).", viewModel.StatusHint.ToPlainText());

            await viewModel.RenameListViewModel.ClearCommand.ExecuteAsync(null);

            Assert.Equal(0, viewModel.ItemCount);
            Assert.True(viewModel.StatusHint.IsEmpty);
        }

        /// <summary>
        /// Verifies Applied Filters preset load/save status is sticky on the main-window hint.
        /// </summary>
        [AvaloniaFact]
        public void AppliedFilters_preset_status_updates_main_window_hint()
        {
            var viewModel = new MainWindowViewModel();
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "ShellPreset",
                Chain = new FilterChain { Steps = [] },
            };

            viewModel.AppliedFiltersViewModel.LoadPreset(preset);

            Assert.Equal("Loaded preset \"ShellPreset\".", viewModel.StatusHint.ToPlainText());
            Assert.All(viewModel.StatusHint.Runs, run => Assert.Null(run.ForegroundResourceKey));

            viewModel.AppliedFiltersViewModel.SavePreset("ShellPresetSaved", description: null, visibleColumns: null);

            Assert.Equal("Saved preset \"ShellPresetSaved\".", viewModel.StatusHint.ToPlainText());
            Assert.All(viewModel.StatusHint.Runs, run => Assert.Null(run.ForegroundResourceKey));
        }

        /// <summary>
        /// Verifies File List sticky status is forwarded to the main-window hint.
        /// </summary>
        [AvaloniaFact]
        public void FileList_status_updates_main_window_hint()
        {
            var viewModel = new MainWindowViewModel();

            viewModel.FileListViewModel.LastStatusMessage = StatusBarText.Neutral("Copied 1 path(s).");

            Assert.Equal("Copied 1 path(s).", viewModel.StatusHint.ToPlainText());
            Assert.All(viewModel.StatusHint.Runs, run => Assert.Null(run.ForegroundResourceKey));
        }

        /// <summary>
        /// Verifies stopping GO during preview publishes a warning Stopped status.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_preview_stop_sets_stopped_status()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var paths = new List<string>();
            for (var i = 0; i < 40; i++)
            {
                var path = Path.Combine(dir, $"f{i:D2}.txt");
                await File.WriteAllTextAsync(path, "x");
                paths.Add(path);
            }

            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync(paths).ConfigureAwait(true);

            var commitStarted = await _CancelWhenBusyAsync(
                    viewModel.RenameListViewModel,
                    RenameListProgressOperation.Preview,
                    () => viewModel.RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("f", "g")))
                )
                .ConfigureAwait(true);

            Assert.False(commitStarted);
            Assert.Equal("Stopped.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
            Assert.Equal(
                StatusBarText.WarningForegroundResourceKey,
                viewModel.RenameListViewModel.LastStatusMessage.Runs[0].ForegroundResourceKey
            );
            Assert.Equal("Stopped.", viewModel.StatusHint.ToPlainText());
        }

        /// <summary>
        /// Verifies stopping GO mid-commit publishes a warning with renamed count.
        /// </summary>
        [AvaloniaFact]
        public async Task Go_commit_stop_sets_stopped_status()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var paths = new List<string>();
            for (var i = 0; i < 20; i++)
            {
                var path = Path.Combine(dir, $"item{i:D2}.txt");
                await File.WriteAllTextAsync(path, "x");
                paths.Add(path);
            }

            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync(paths).ConfigureAwait(true);

            var commitStarted = await _CancelWhenBusyAsync(
                    viewModel.RenameListViewModel,
                    RenameListProgressOperation.Commit,
                    () => viewModel.RenameListViewModel.GoAsync(_Chain(_PrefixReplacer("item", "done")))
                )
                .ConfigureAwait(true);

            Assert.True(commitStarted);
            var status = viewModel.RenameListViewModel.LastStatusMessage;
            Assert.StartsWith("Stopped.", status.ToPlainText());
            Assert.Equal(StatusBarText.WarningForegroundResourceKey, status.Runs[0].ForegroundResourceKey);
            Assert.Equal(status.ToPlainText(), viewModel.StatusHint.ToPlainText());
        }

        /// <summary>
        /// Cancels Rename List progress when <paramref name="operation"/> becomes busy.
        /// </summary>
        private static async Task<T> _CancelWhenBusyAsync<T>(
            RenameListViewModel renameList,
            RenameListProgressOperation operation,
            Func<Task<T>> action
        )
        {
            void OnProgressChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (
                    e.PropertyName is nameof(RenameListProgressViewModel.IsBusy)
                    && renameList.IsBusy
                    && renameList.Progress.Operation == operation
                )
                {
                    renameList.Progress.CancelCommand.Execute(null);
                }
            }

            renameList.Progress.PropertyChanged += OnProgressChanged;
            try
            {
                return await action().ConfigureAwait(true);
            }
            finally
            {
                renameList.Progress.PropertyChanged -= OnProgressChanged;
            }
        }

        private static FilterChain _Chain(params ReplacerFilter[] filters)
        {
            return new FilterChain
            {
                Steps = [.. filters.Select(filter => new FilterChainStep(Enabled: true, Filter: filter))],
            };
        }

        private static ReplacerFilter _PrefixReplacer(string find, string replacement)
        {
            return new ReplacerFilter(
                Target: new FilePrefixTarget(),
                Options: new ReplacerOptions(
                    Find: find,
                    Replacement: replacement,
                    Match: ReplacerMatchOptions.ForReplacer
                )
            );
        }
    }
}
