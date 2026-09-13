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
        /// Verifies Options is enabled while Undo/Log remain stubs.
        /// </summary>
        [AvaloniaFact]
        public void ShowOptionsCommand_is_enabled()
        {
            var viewModel = new MainWindowViewModel(persistSession: true);
            Assert.True(viewModel.ShowOptionsCommand.CanExecute(null));
            Assert.False(viewModel.UndoLastCommand.CanExecute(null));
            Assert.False(viewModel.ShowLogCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the Options dialog constructs remember checkboxes, prompts radios, double-click radios,
        /// and Rename List add-mode controls.
        /// </summary>
        [AvaloniaFact]
        public void OptionsDialog_shows_remember_prompts_double_click_and_add_mode()
        {
            var dialogVm = new OptionsDialogViewModel();
            var dialog = new OptionsDialog(dialogVm);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();

            try
            {
                var labels = dialog
                    .GetVisualDescendants()
                    .OfType<CompactCheckBox>()
                    .Select(box => box.Content?.ToString())
                    .ToList();

                Assert.Contains("Save File List last position", labels);
                Assert.Contains("Remember window size and position", labels);
                Assert.Contains("Add folder contents", labels);

                var radioLabels = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => radio.Content?.ToString())
                    .ToList();
                Assert.Contains("Fewer", radioLabels);
                Assert.Contains("Normal", radioLabels);
                Assert.Contains("More", radioLabels);
                Assert.Contains("Open", radioLabels);
                Assert.Contains("Add to Rename List", radioLabels);
                Assert.Contains("Files", radioLabels);
                Assert.Contains("Folders", radioLabels);
                Assert.Contains("Files and folders", radioLabels);

                var texts = dialog.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToList();
                Assert.Contains("Add to Rename List:", texts);

                var checkTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactCheckBox>()
                    .Select(box => ToolTip.GetTip(box)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsRememberLastFolder, checkTips);
                Assert.Contains(AppTips.OptionsRememberWindowState, checkTips);
                Assert.Contains(AppTips.OptionsAddFolderContents, checkTips);

                var radioTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Select(radio => ToolTip.GetTip(radio)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsConfirmationFewer, radioTips);
                Assert.Contains(AppTips.OptionsConfirmationNormal, radioTips);
                Assert.Contains(AppTips.OptionsConfirmationMore, radioTips);
                Assert.Contains(AppTips.OptionsDoubleClickOpen, radioTips);
                Assert.Contains(AppTips.OptionsDoubleClickAdd, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFiles, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFolders, radioTips);
                Assert.Contains(AppTips.OptionsAddModeFilesAndFolders, radioTips);
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
        /// Verifies OK commits drafts and invokes config save.
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
            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList.AddMode);
            Assert.False(ConfigStore.RenameList.AddFolderContents);

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
            Assert.True(ConfigStore.FileList.RememberLastFolder);
            Assert.True(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.Fewer, ConfigStore.Ui.ConfirmationPrompts);
            Assert.False(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Files, ConfigStore.RenameList.AddMode);
            Assert.True(ConfigStore.RenameList.AddFolderContents);

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
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = true };
            ConfigStore.FileList = new FileListPrefs { RememberLastFolder = true, DoubleClickAddsToRenameList = false };
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.RenameList = new RenameListPrefs
            {
                AddMode = RenameListAddMode.Files,
                AddFolderContents = true,
            };
        }

        /// <summary>
        /// Sets every Options draft away from <see cref="_SeedOptionsPrefs"/> so commit vs cancel is observable.
        /// </summary>
        private static void _MutateDraftAwayFromSeed(OptionsDialogViewModel vm)
        {
            vm.RememberLastFolder = false;
            vm.RememberWindowState = false;
            vm.ConfirmationPrompts = ConfirmationPrompts.More;
            vm.DoubleClickAddsToRenameList = true;
            vm.AddMode = RenameListAddMode.Folders;
            vm.AddFolderContents = false;
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
