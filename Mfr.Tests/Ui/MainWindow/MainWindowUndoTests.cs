using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Main-window Undo Last command enablement, confirm gate, and prepare-session outcomes.
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
            await File.WriteAllTextAsync(source, "alpha");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();

            Assert.False(viewModel.UndoLastCommand.CanExecute(null));

            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);
            viewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Replacer"));
            viewModel.AppliedFiltersViewModel.Steps[0].SetFilter(UndoPrepareTestUi.PrefixReplacer("alpha", "renamed"));
            ConfigStore.Ui.SuppressedConfirmations =
            [
                ConfirmationKind.GoWithPreviewErrors,
                ConfirmationKind.UndoRename,
            ];

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(viewModel.UndoLastCommand.CanExecute(null));

            RenameLogStore.ClearLastOperation();
            viewModel.UndoLastCommand.NotifyCanExecuteChanged();
            Assert.False(viewModel.UndoLastCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Undo Last prepares a preview session without changing disk or refreshing the File List.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_prepares_session_without_commit_or_file_list_refresh()
        {
            var (viewModel, source, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture)
                .ConfigureAwait(true);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            Assert.True(File.Exists(destination));
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");
            Assert.Single(viewModel.AppliedFiltersViewModel.Steps);

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");
            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.Empty(viewModel.AppliedFiltersViewModel.Steps);
            Assert.Contains("Prepared undo", viewModel.StatusHint.ToPlainText());
            Assert.Contains("press GO", viewModel.StatusHint.ToPlainText());
            Assert.Equal(RenameListProgressOperation.Add, viewModel.RenameListViewModel.Progress.Operation);

            var prepared = Assert.Single(viewModel.RenameListViewModel.Entries);
            Assert.Equal(destination, prepared.EngineItem.Original.FullPath);
            Assert.Equal(source, prepared.EngineItem.Preview.FullPath);
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.True(RenameLogStore.LastOperation.HasUndoableEntries);

            var visibleKeys = viewModel.RenameListViewModel.VisibleColumns.Select(column => column.Key).ToList();
            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                ],
                visibleKeys
            );
            Assert.True(
                prepared.EngineItem.IsOverridden(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
                )
            );
        }

        /// <summary>
        /// Verifies prepare with Auto-Preview on still clears filters and keeps sticky reverse paths.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_with_auto_preview_clears_filters_and_keeps_sticky_preview()
        {
            var (viewModel, source, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture, disableAutoPreview: false)
                .ConfigureAwait(true);
            Assert.True(File.Exists(destination));
            Assert.True(viewModel.RenameListViewModel.IsAutoPreview);

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);
            await viewModel.WaitForPendingPreviewAsync().ConfigureAwait(true);

            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
            Assert.Empty(viewModel.AppliedFiltersViewModel.Steps);
            Assert.Contains("Prepared undo", viewModel.StatusHint.ToPlainText());

            var prepared = Assert.Single(viewModel.RenameListViewModel.Entries);
            Assert.Equal(destination, prepared.EngineItem.Original.FullPath);
            Assert.Equal(source, prepared.EngineItem.Preview.FullPath);
        }

        /// <summary>
        /// Verifies GO after PrepareUndo restores the prior name and refreshes the File List.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_then_Go_restores_rename_and_refreshes_file_list()
        {
            var (viewModel, source, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture)
                .ConfigureAwait(true);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            Assert.True(File.Exists(destination));

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);
            Assert.True(File.Exists(destination));

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);

            Assert.True(File.Exists(source));
            Assert.False(File.Exists(destination));
            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "alpha.txt");
            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "renamed.txt");
            Assert.True(RenameLogStore.LastOperation!.IsUndo);
        }

        /// <summary>
        /// Verifies prepare under Before/After Mode replaces columns without leaving companion expand.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_with_ab_mode_replaces_columns_without_companion_expand()
        {
            var (viewModel, _, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture)
                .ConfigureAwait(true);
            Assert.True(File.Exists(destination));

            viewModel.RenameListViewModel.IsAbModeEnabled = true;
            Assert.True(viewModel.RenameListViewModel.IsAbModeEnabled);
            Assert.All(viewModel.RenameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));

            await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.False(viewModel.RenameListViewModel.IsAbModeEnabled);
            var visibleKeys = viewModel.RenameListViewModel.VisibleColumns.Select(column => column.Key).ToList();
            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                ],
                visibleKeys
            );
            Assert.DoesNotContain(
                visibleKeys,
                key => key == RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
            );
        }

        /// <summary>
        /// Verifies confirm Undo when not suppressed and decline aborts without filesystem changes.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_confirm_decline_aborts()
        {
            var (viewModel, source, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture)
                .ConfigureAwait(true);

            ConfirmationPolicy.ClearSuppressions();
            var confirmCalls = 0;
            viewModel.RenameListViewModel.UiHooks = new RenameListUiHooks
            {
                ConfirmUndoRenameAsync = () =>
                {
                    confirmCalls++;
                    return Task.FromResult(false);
                },
            };

            var started = await viewModel.RenameListViewModel.PrepareUndoLastAsync().ConfigureAwait(true);

            Assert.False(started);
            Assert.Equal(1, confirmCalls);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
        }

        /// <summary>
        /// Verifies a suppressed UndoRename skips the Undo confirm dialog.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoLast_suppressed_skips_confirm()
        {
            var (viewModel, source, destination) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture)
                .ConfigureAwait(true);

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
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
            Assert.Contains("Prepared undo", viewModel.StatusHint.ToPlainText());
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

            var started = await viewModel.RenameListViewModel.PrepareUndoLastAsync().ConfigureAwait(true);

            Assert.False(started);
            Assert.Equal("Nothing to undo.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
        }

        /// <summary>
        /// Verifies Undo status when DestinationPath files are already gone.
        /// </summary>
        [AvaloniaFact]
        public async Task Undo_missing_paths_status()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var missing = Path.Combine(dir, "gone.txt");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);

            var log = new RenameLog(
                CommittedAt: DateTimeOffset.UtcNow,
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: missing,
                        OriginalPath: Path.Combine(dir, "old.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "old", "gone")]
                    ),
                ]
            );

            var started = await viewModel.RenameListViewModel.PrepareUndoAsync(log).ConfigureAwait(true);

            Assert.True(started);
            Assert.Equal(
                "Could not load 1 item(s) for undo (paths missing).",
                viewModel.RenameListViewModel.LastStatusMessage.ToPlainText()
            );
        }
    }
}
