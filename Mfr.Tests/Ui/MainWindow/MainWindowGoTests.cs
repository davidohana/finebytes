using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Replace;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window GO command state and Rename List commit orchestration.
    /// </summary>
    public sealed class MainWindowGoTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

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
        /// Verifies the bound main-window GO command previews the live filters and performs the filesystem rename.
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

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(File.Exists(source));
            Assert.True(File.Exists(destination));
            Assert.Equal(RenameListProgressOperation.Commit, viewModel.RenameListViewModel.Progress.Operation);
        }

        /// <summary>
        /// Verifies declining the preview-error warning aborts before any valid rename is committed.
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
            Assert.Contains("could not be renamed", viewModel.RenameListViewModel.LastGoStatus);
            Assert.Contains("Show Rename Error", viewModel.RenameListViewModel.LastGoStatus);
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
            Assert.Equal("Renamed 1 item(s).", viewModel.RenameListViewModel.LastGoStatus);
            Assert.True(File.Exists(destination));
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
