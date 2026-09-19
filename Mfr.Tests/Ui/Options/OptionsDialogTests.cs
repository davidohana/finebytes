using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Resources;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.Options;
using Mfr.App.Ui.Views;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.Options;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.Options
{
    /// <summary>
    /// Headless tests for Options command enablement, dialog open, and OK/Cancel persist path.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class OptionsDialogTests
    {
        public OptionsDialogTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies Options and Log are enabled while Undo Last stays disabled with no last op.
        /// </summary>
        [AvaloniaFact]
        public void ShowOptionsCommand_is_enabled()
        {
            var viewModel = new MainWindowViewModel(persistSession: true);
            Assert.True(viewModel.ShowOptionsCommand.CanExecute(null));
            Assert.False(viewModel.UndoLastCommand.CanExecute(null));
            Assert.True(viewModel.ShowLogCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the Options dialog General and Undo tabs host Session, Confirmations, File List,
        /// Rename List, Location, and Undo &amp; Rename Log retention controls.
        /// </summary>
        [AvaloniaFact]
        public void OptionsDialog_shows_general_and_undo_tabs()
        {
            var dialogVm = new OptionsDialogViewModel();
            var dialog = new OptionsDialog(dialogVm);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();

            try
            {
                var tabs = dialog.FindControl<TabControl>("OptionsTabs");
                Assert.NotNull(tabs);
                var tabHeaders = tabs.Items.OfType<TabItem>().Select(item => item.Header?.ToString()).ToList();
                Assert.Equal(["General", "Undo & Rename Log"], tabHeaders);
                Assert.Equal(0, tabs.SelectedIndex);

                var labels = dialog
                    .GetVisualDescendants()
                    .OfType<CompactCheckBox>()
                    .Select(box => box.Content?.ToString())
                    .ToList();

                Assert.Contains("Remember last folder", labels);
                Assert.Contains("Remember window size and position", labels);
                Assert.Contains("Add folder contents", labels);

                var radioLabels = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => radio.Content?.ToString())
                    .ToList();
                Assert.DoesNotContain("Fewer", radioLabels);
                Assert.DoesNotContain("Normal", radioLabels);
                Assert.DoesNotContain("More", radioLabels);
                Assert.Contains("Open", radioLabels);
                Assert.Contains("Add to Rename List", radioLabels);
                Assert.Contains("Add files", radioLabels);
                Assert.Contains("Add folders", radioLabels);
                Assert.Contains("Add files and folders", radioLabels);

                var groupHeaders = dialog
                    .GetVisualDescendants()
                    .OfType<FieldsetGroup>()
                    .Select(group => group.Header?.ToString())
                    .ToList();
                Assert.Contains("Session", groupHeaders);
                Assert.Contains("Confirmations", groupHeaders);
                Assert.Contains("File List", groupHeaders);
                Assert.Contains("Rename List", groupHeaders);
                Assert.Contains("Location", groupHeaders);
                Assert.DoesNotContain("Undo & Rename Log", groupHeaders);

                var confirmationsBlurb = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .First(block =>
                        block.Text is not null
                        && block.Text.Contains(
                            "Confirmation dialogs you chose not to see again",
                            StringComparison.Ordinal
                        )
                    );
                Assert.Equal(AppTips.OptionsConfirmations, ToolTip.GetTip(confirmationsBlurb)?.ToString());

                var summary = dialog.FindControl<TextBlock>("SuppressedConfirmationsSummaryText");
                Assert.NotNull(summary);
                Assert.Equal("No confirmations are currently suppressed.", summary.Text);

                var resetButton = dialog.FindControl<Button>("ResetConfirmationsButton");
                Assert.NotNull(resetButton);
                Assert.Equal("Reset confirmations", resetButton.Content?.ToString());
                Assert.Equal(AppTips.OptionsResetConfirmations, ToolTip.GetTip(resetButton)?.ToString());

                var rowLabels = dialog
                    .GetVisualDescendants()
                    .OfType<FilterEditorLabeledRow>()
                    .Select(row => row.Label)
                    .ToList();
                Assert.Contains("Double-click:", rowLabels);
                Assert.Contains("GeoNames username:", rowLabels);
                Assert.DoesNotContain("Add:", rowLabels);

                var checkTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactCheckBox>()
                    .Select(box => ToolTip.GetTip(box)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsRememberLastFolder, checkTips);
                Assert.Contains(AppTips.OptionsRememberWindowState, checkTips);
                Assert.Contains("dialog", AppTips.OptionsRememberWindowState, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(AppTips.OptionsAddFolderContents, checkTips);
                Assert.Contains(AppTips.OptionsIncludeHidden, checkTips);
                Assert.Contains(AppTips.OptionsRememberColumnWidths, checkTips);

                var radioTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => ToolTip.GetTip(radio)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsDoubleClickOpen, radioTips);
                Assert.Contains(AppTips.OptionsDoubleClickAdd, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFiles, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFolders, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFilesAndFolders, radioTips);

                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();

                var undoGroupHeaders = dialog
                    .GetVisualDescendants()
                    .OfType<FieldsetGroup>()
                    .Select(group => group.Header?.ToString())
                    .ToList();
                Assert.Contains("Undo & Rename Log", undoGroupHeaders);
                Assert.DoesNotContain("Session", undoGroupHeaders);

                var undoRadioLabels = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => radio.Content?.ToString())
                    .ToList();
                Assert.Contains("Rename Log disabled (Undo for last renaming operation only)", undoRadioLabels);
                Assert.Contains("Limited to:", undoRadioLabels);
                Assert.Contains("Unlimited", undoRadioLabels);

                Assert.NotNull(dialog.FindControl<CompactNumericUpDown>("RenameLogLimitSpinner"));

                var undoRadioTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => ToolTip.GetTip(radio)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsRenameLogDisabled, undoRadioTips);
                Assert.Contains(AppTips.OptionsRenameLogLimited, undoRadioTips);
                Assert.Contains(AppTips.OptionsRenameLogUnlimited, undoRadioTips);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies Reset confirmations clears the draft suppress list without touching ConfigStore.
        /// </summary>
        [AvaloniaFact]
        public void OptionsDialog_ResetConfirmations_clears_draft_only()
        {
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.ClearFilterChain];
            var dialogVm = new OptionsDialogViewModel();
            var dialog = new OptionsDialog(dialogVm);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();

            try
            {
                Assert.Equal([ConfirmationKind.ClearFilterChain], dialogVm.SuppressedConfirmations);
                Assert.Equal("1 confirmation is currently suppressed.", dialogVm.SuppressedConfirmationsSummary);

                var summary = dialog.FindControl<TextBlock>("SuppressedConfirmationsSummaryText");
                Assert.NotNull(summary);
                Assert.Equal("1 confirmation is currently suppressed.", summary.Text);

                var resetButton = dialog.FindControl<Button>("ResetConfirmationsButton");
                Assert.NotNull(resetButton);
                Assert.NotNull(resetButton.Command);
                Assert.True(resetButton.Command.CanExecute(null));
                resetButton.Command.Execute(null);
                Dispatcher.UIThread.RunJobs();

                Assert.Empty(dialogVm.SuppressedConfirmations);
                Assert.Equal("No confirmations are currently suppressed.", dialogVm.SuppressedConfirmationsSummary);
                Assert.Equal("No confirmations are currently suppressed.", summary.Text);
                Assert.Equal([ConfirmationKind.ClearFilterChain], ConfigStore.Options.SuppressedConfirmations);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies OK closes the dialog with <see langword="true"/>.
        /// </summary>
        [AvaloniaFact]
        public async Task OptionsDialog_Ok_returns_true()
        {
            var owner = new Window();
            owner.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new OptionsDialog(new OptionsDialogViewModel());
            var resultTask = dialog.ShowDialog<bool?>(owner);
            Dispatcher.UIThread.RunJobs();

            _ClickAccept(dialog);
            Assert.True(await resultTask);
            owner.Close();
        }

        /// <summary>
        /// Verifies Cancel closes the dialog with <see langword="false"/>.
        /// </summary>
        [AvaloniaFact]
        public async Task OptionsDialog_Cancel_returns_false()
        {
            var owner = new Window();
            owner.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new OptionsDialog(new OptionsDialogViewModel());
            var resultTask = dialog.ShowDialog<bool?>(owner);
            Dispatcher.UIThread.RunJobs();

            _ClickCancel(dialog);
            Assert.False(await resultTask);
            owner.Close();
        }

        /// <summary>
        /// Verifies OK commits drafts, invokes config save, and refreshes Rename List add can-execute.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_Ok_commits_and_saves_config()
        {
            _SeedOptionsPrefs();

            var saved = false;
            OptionsDialogViewModel? shown = null;
            var (viewModel, window) = _ShowMainWindow(
                persistSession: true,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        shown = vm;
                        _MutateDraftAwayFromSeed(vm);
                        return Task.FromResult<bool?>(true);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.NotNull(shown);
            Assert.True(saved);
            Assert.False(ConfigStore.Options.RememberLastFolder);
            Assert.False(ConfigStore.Options.RememberWindowState);
            Assert.Empty(ConfigStore.Options.SuppressedConfirmations);
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.Options.AddMode);
            Assert.False(ConfigStore.Options.AddFolderContents);
            Assert.Equal(0, ConfigStore.RenameLog.Limit);
            Assert.True(ConfigStore.Options.IncludeHidden);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies OK with an explicit prune directory trims on-disk logs to the committed limit.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_Ok_prunes_rename_logs_when_hooks_path_set()
        {
            using var tempDirs = new TempDirectoryFixture();
            var logDir = tempDirs.CreateTempDir();
            var baseTime = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 4; i++)
            {
                var path = Path.Combine(logDir, $"{i:D3}{RenameLogStore.FileExtension}");
                File.WriteAllText(path, "x");
                File.SetCreationTimeUtc(path, baseTime.AddMinutes(i));
            }

            _SeedOptionsPrefs();
            var (viewModel, window) = _ShowMainWindow(
                persistSession: true,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        vm.RenameLogLimitedCount = 2;
                        vm.RenameLogRetentionMode = RenameLogRetentionMode.Limited;
                        return Task.FromResult<bool?>(true);
                    },
                    SaveConfig = () => { },
                    PruneRenameLogDirectoryPath = logDir,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.Equal(2, ConfigStore.RenameLog.Limit);
            var remaining = Directory
                .EnumerateFiles(logDir, $"*{RenameLogStore.FileExtension}")
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            Assert.Equal(["002.mfrlog", "003.mfrlog"], remaining);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies Cancel leaves prefs memory and does not save.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_Cancel_does_not_commit_or_save()
        {
            _SeedOptionsPrefs();

            var saved = false;
            var (viewModel, window) = _ShowMainWindow(
                persistSession: true,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        _MutateDraftAwayFromSeed(vm);
                        return Task.FromResult<bool?>(false);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.False(saved);
            Assert.True(ConfigStore.Options.RememberLastFolder);
            Assert.True(ConfigStore.Options.RememberWindowState);
            Assert.Equal([ConfirmationKind.GoWithPreviewErrors], ConfigStore.Options.SuppressedConfirmations);
            Assert.False(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Files, ConfigStore.Options.AddMode);
            Assert.True(ConfigStore.Options.AddFolderContents);
            Assert.False(ConfigStore.Options.IncludeHidden);
            Assert.Equal(10, ConfigStore.RenameLog.Limit);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies Options OK reloads the File List so IncludeHidden visibility applies without restart.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_Ok_refreshes_file_list_for_include_hidden()
        {
            using var tempDirs = new TempDirectoryFixture();
            var dir = tempDirs.CreateTempDir();
            var hiddenName = OperatingSystem.IsWindows() ? "secret.txt" : ".secret.txt";
            var hiddenPath = Path.Combine(dir, hiddenName);
            File.WriteAllText(hiddenPath, "hidden");
            if (OperatingSystem.IsWindows())
            {
                File.SetAttributes(hiddenPath, FileAttributes.Hidden);
            }

            _SeedOptionsPrefs();
            var (viewModel, window) = _ShowMainWindow(
                persistSession: true,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        vm.IncludeHidden = true;
                        return Task.FromResult<bool?>(true);
                    },
                    SaveConfig = () => { },
                }
            );
            viewModel.FileListViewModel.NavigateTo(dir);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, e => e.Name == hiddenName);

            await _InvokeShowOptionsAsync(viewModel);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);

            Assert.True(ConfigStore.Options.IncludeHidden);
            Assert.Contains(viewModel.FileListViewModel.Entries, e => e.Name == hiddenName);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies Options is a no-op when <see cref="MainWindowViewModel.PersistSession"/> is false.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_when_persist_session_false_is_noop()
        {
            var shown = false;
            var (viewModel, window) = _ShowMainWindow(
                persistSession: false,
                new OptionsDialogHooks
                {
                    Show = _ =>
                    {
                        shown = true;
                        return Task.FromResult<bool?>(true);
                    },
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.False(shown);
            Assert.False(viewModel.PersistSession);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Seeds ConfigStore Options-related prefs to values distinct from the draft mutation used in OK/Cancel tests.
        /// </summary>
        private static void _SeedOptionsPrefs()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Options.RememberLastFolder = true;
            ConfigStore.Options.DoubleClickAddsToRenameList = false;
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.GoWithPreviewErrors];
            ConfigStore.Options.AddMode = RenameListAddMode.Files;
            ConfigStore.Options.AddFolderContents = true;
            ConfigStore.Options.IncludeHidden = false;
            ConfigStore.RenameLog.Limit = RenameLogConfig.DefaultLimit;
        }

        /// <summary>
        /// Sets every Options draft away from <see cref="_SeedOptionsPrefs"/> so commit vs cancel is observable.
        /// </summary>
        private static void _MutateDraftAwayFromSeed(OptionsDialogViewModel vm)
        {
            vm.RememberLastFolder = false;
            vm.RememberWindowState = false;
            vm.SuppressedConfirmations = [];
            vm.DoubleClickAddsToRenameList = true;
            vm.AddMode = RenameListAddMode.Folders;
            vm.AddFolderContents = false;
            vm.IncludeHidden = true;
            vm.RenameLogRetentionMode = RenameLogRetentionMode.Disabled;
        }

        private static (MainWindowViewModel ViewModel, AppMainWindow Window) _ShowMainWindow(
            bool persistSession,
            OptionsDialogHooks hooks
        )
        {
            var viewModel = new MainWindowViewModel(persistSession: persistSession);
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
                OptionsDialogHooks = hooks,
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (viewModel, window);
        }

        private static async Task _InvokeShowOptionsAsync(MainWindowViewModel viewModel)
        {
            viewModel.ShowOptions();
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Clicks the Options accept button.
        /// </summary>
        private static void _ClickAccept(OptionsDialog dialog)
        {
            ModalOkCancelFooterAccess.RequireAcceptButton(dialog).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Clicks the Options Cancel button.
        /// </summary>
        private static void _ClickCancel(OptionsDialog dialog)
        {
            ModalOkCancelFooterAccess.RequireCancelButton(dialog).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }
    }
}
