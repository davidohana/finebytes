using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Replace;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window Undo Last command enablement, confirm gate, and status outcomes.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class MainWindowUndoTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        public MainWindowUndoTests()
        {
            ConfigStoreTestReset.LoadEmpty();
            RenameLogStore.ClearLastOperation();
            ConfigStore.RenameLog.Limit = 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameLogStore.ClearLastOperation();
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies Undo Last stays disabled until a GO captures a last operation.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLastCommand_enabled_after_go_and_disabled_when_empty()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var destination = Path.Combine(dir, "renamed.txt");
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();

            Assert.False(viewModel.UndoLastCommand.CanExecute(null));

            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);
            viewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Replacer"));
            viewModel.AppliedFiltersViewModel.Steps[0].SetFilter(_PrefixReplacer("alpha", "renamed"));
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(destination));
            Assert.True(viewModel.UndoLastCommand.CanExecute(null));

            RenameLogStore.ClearLastOperation();
            viewModel.UndoLastCommand.NotifyCanExecuteChanged();
            Assert.False(viewModel.UndoLastCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Undo Last restores the prior name, refreshes the File List, and publishes status.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_restores_rename_and_refreshes_file_list()
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
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);
            Assert.True(File.Exists(destination));
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(source));
            Assert.False(File.Exists(destination));
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");
            Assert.Contains("Undid", viewModel.StatusHint.ToPlainText());
            Assert.Equal(RenameListProgressOperation.Commit, viewModel.RenameListViewModel.Progress.Operation);
        }

        /// <summary>
        /// Verifies Normal prompts confirm Undo and decline aborts without filesystem changes.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_normal_confirm_decline_aborts()
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
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Normal;
            var confirmCalls = 0;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmUndoRenameAsync = () =>
                {
                    confirmCalls++;
                    return Task.FromResult(false);
                },
            };

            var started = await viewModel.RenameListViewModel.UndoLastAsync().ConfigureAwait(true);

            Assert.False(started);
            Assert.Equal(1, confirmCalls);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
        }

        /// <summary>
        /// Verifies Fewer skips the Undo confirm dialog.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_fewer_skips_confirm()
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
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            var confirmCalls = 0;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmUndoRenameAsync = () =>
                {
                    confirmCalls++;
                    return Task.FromResult(false);
                },
            };

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.Equal(0, confirmCalls);
            Assert.True(File.Exists(source));
            Assert.False(File.Exists(destination));
        }

        /// <summary>
        /// Verifies calling Undo Last with no last operation publishes "Nothing to undo."
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_nothing_to_undo_status()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.RenameListViewModel.DisableAutoPreview();
            RenameLogStore.ClearLastOperation();

            var started = await viewModel.RenameListViewModel.UndoLastAsync().ConfigureAwait(true);

            Assert.False(started);
            Assert.Equal("Nothing to undo.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
        }

        private static ReplacerFilter _PrefixReplacer(string find, string replacement)
        {
            return new ReplacerFilter(
                Target: new FilePrefixTarget(),
                Options: new ReplacerOptions(
                    Find: find,
                    Replacement: replacement,
                    Match: new ReplacerMatchOptions(
                        Mode: ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: false,
                        WholeWord: false
                    )
                )
            );
        }
    }
}
