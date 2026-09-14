using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.Services.RenameLog;
using Mfr.App.Ui.ViewModels.LogDialog;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.Views;
using Mfr.App.Ui.Views.LogDialog;
using Mfr.Filters.Replace;
using Mfr.Tests.Ui.AppliedFilters;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.LogDialog
{
    /// <summary>
    /// Headless smoke tests for the Rename Log dialog chrome and Log-window Undo path.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameLogDialogTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        public RenameLogDialogTests()
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
        /// Verifies the dialog constructs with list, details, Undo, Erase, and Close.
        /// </summary>
        [AvaloniaFact]
        public void RenameLogDialog_shows_list_and_actions()
        {
            RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("a.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "a", "b")],
                        DestinationPath: TestPaths.Absolute("b.txt"),
                        IsFolder: false
                    ),
                ],
                limit: 0
            );

            var dialogVm = new RenameLogDialogViewModel(directoryPath: _tempDirectoryFixture.CreateTempDir());
            var dialog = new RenameLogDialog(dialogVm);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();

            try
            {
                Assert.Equal("Rename Log", dialog.Title);
                Assert.NotNull(dialog.FindControl<ListBox>("LogsList"));
                Assert.NotNull(dialog.FindControl<TextBox>("DetailsBox"));
                Assert.NotNull(dialog.FindControl<Button>("UndoButton"));
                Assert.NotNull(dialog.FindControl<Button>("EraseButton"));
                Assert.NotNull(dialog.FindControl<Button>("CloseButton"));
                Assert.NotNull(RenameLogStore.LastOperation);
                Assert.Contains(
                    RenameLogDisplay.FormatListTitle(RenameLogStore.LastOperation.CommittedAt),
                    dialogVm.Items.Select(item => item.Title)
                );
                Assert.Contains("b.txt", dialogVm.DetailsText);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies Escape closes the dialog via the Close button's <c>IsCancel</c> wiring.
        /// </summary>
        [AvaloniaFact]
        public void Escape_Closes_Dialog()
        {
            var dialog = new RenameLogDialog(new RenameLogDialogViewModel(_tempDirectoryFixture.CreateTempDir()));
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(dialog.IsVisible);

            dialog.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Escape,
                    Source = dialog,
                }
            );
            Dispatcher.UIThread.RunJobs();

            Assert.False(dialog.IsVisible);
        }

        /// <summary>
        /// Verifies Delete on the log list erases the selected disk row (gesture → EraseCommand).
        /// </summary>
        [AvaloniaFact]
        public async Task Delete_On_List_Erases_Selected_Disk_Log()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var written = RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("a.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "a", "b")],
                        DestinationPath: TestPaths.Absolute("b.txt"),
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 10
            );
            Assert.NotNull(written);
            RenameLogStore.ClearLastOperation();

            var dialogVm = new RenameLogDialogViewModel(logDir);
            var dialog = new RenameLogDialog(dialogVm);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                Assert.Single(dialogVm.Items);
                Assert.True(File.Exists(written));

                var list = dialog.FindControl<ListBox>("LogsList");
                Assert.NotNull(list);
                list.RaiseEvent(
                    new KeyEventArgs
                    {
                        RoutedEvent = InputElement.KeyDownEvent,
                        Key = Key.Delete,
                        Source = list,
                    }
                );
                Dispatcher.UIThread.RunJobs();
                await Task.Yield();
                Dispatcher.UIThread.RunJobs();

                Assert.Empty(dialogVm.Items);
                Assert.False(File.Exists(written));
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies Show Log opens via the main window and returns without undo when dismissed.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowLog_opens_dialog_via_hooks()
        {
            RenameLogDialogViewModel? shown = null;
            var viewModel = new MainWindowViewModel(persistSession: true);
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
                RenameLogDialogHooks = new RenameLogDialogHooks
                {
                    Show = vm =>
                    {
                        shown = vm;
                        return Task.FromResult<bool?>(false);
                    },
                },
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            viewModel.ShowLog();
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(shown);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies undoing a loaded disk log restores the prior name (same path as Log-window Undo).
        /// </summary>
        [AvaloniaFact]
        public async Task UndoFromLog_disk_log_restores_rename()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var destination = Path.Combine(dir, "renamed.txt");
            await File.WriteAllTextAsync(source, "alpha");

            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.DisableAutoPreview();
            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);
            viewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Replacer"));
            viewModel.AppliedFiltersViewModel.Steps[0].SetFilter(_PrefixReplacer("alpha", "renamed"));
            ConfirmationPolicy.Suppress(ConfirmationKind.GoWithPreviewErrors);
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);
            Assert.True(File.Exists(destination));
            Assert.NotNull(RenameLogStore.LastOperation);

            var written = RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: source,
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "alpha", "renamed")],
                        DestinationPath: destination,
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 10
            );
            Assert.NotNull(written);
            RenameLogStore.ClearLastOperation();

            var diskLog = RenameLogStore.TryLoadFile(written);
            Assert.NotNull(diskLog);

            await viewModel.UndoFromLogAsync(diskLog).ConfigureAwait(true);

            Assert.True(File.Exists(source));
            Assert.False(File.Exists(destination));
            Assert.Contains("Undid", viewModel.StatusHint.ToPlainText());
        }

        /// <summary>
        /// Verifies Log-window Undo uses the same confirmation gate as Undo Last.
        /// </summary>
        [AvaloniaFact]
        public async Task UndoFromLog_confirm_decline_aborts()
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
            ConfirmationPolicy.Suppress(ConfirmationKind.GoWithPreviewErrors);
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);
            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);

            var log = RenameLogStore.LastOperation;
            Assert.NotNull(log);

            ConfirmationPolicy.ClearSuppressions();
            var confirmCalls = 0;
            viewModel.RenameListViewModel.UiHooks = new App.Ui.ViewModels.RenameList.RenameListUiHooks
            {
                ConfirmUndoRenameAsync = () =>
                {
                    confirmCalls++;
                    return Task.FromResult(false);
                },
            };

            await viewModel.UndoFromLogAsync(log).ConfigureAwait(true);

            Assert.Equal(1, confirmCalls);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
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
