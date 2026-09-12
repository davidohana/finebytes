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
            var viewModel = new MainWindowViewModel(session: new SessionState());
            Assert.True(viewModel.ShowOptionsCommand.CanExecute(null));
            Assert.False(viewModel.UndoLastCommand.CanExecute(null));
            Assert.False(viewModel.ShowLogCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies the Options dialog constructs remember checkboxes, prompts radios, and double-click radios.
        /// </summary>
        [AvaloniaFact]
        public void OptionsDialog_shows_remember_prompts_and_double_click()
        {
            var dialogVm = new OptionsDialogViewModel(new SessionState());
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

                var checkTips = dialog
                    .GetVisualDescendants()
                    .OfType<CompactCheckBox>()
                    .Select(box => ToolTip.GetTip(box)?.ToString())
                    .ToList();
                Assert.Contains(AppTips.OptionsRememberLastFolder, checkTips);
                Assert.Contains(AppTips.OptionsRememberWindowState, checkTips);

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

            var dialog = new OptionsDialog(new OptionsDialogViewModel(new SessionState()));
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

            var dialog = new OptionsDialog(new OptionsDialogViewModel(new SessionState()));
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
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = true },
                FileList = new SessionStateFileList { RememberLastFolder = true },
            };
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = false;

            var saved = false;
            OptionsDialogViewModel? shown = null;
            var (viewModel, window) = _ShowMainWindow(
                session,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        shown = vm;
                        vm.RememberLastFolder = false;
                        vm.RememberWindowState = false;
                        vm.ConfirmationPrompts = ConfirmationPrompts.More;
                        vm.DoubleClickAddsToRenameList = true;
                        return Task.FromResult<bool?>(true);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.NotNull(shown);
            Assert.True(saved);
            Assert.False(session.FileList.RememberLastFolder);
            Assert.False(session.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies Cancel leaves session/config and does not save.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_Cancel_does_not_commit_or_save()
        {
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = true },
                FileList = new SessionStateFileList { RememberLastFolder = true },
            };
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = false;

            var saved = false;
            var (viewModel, window) = _ShowMainWindow(
                session,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        vm.RememberLastFolder = false;
                        vm.ConfirmationPrompts = ConfirmationPrompts.More;
                        vm.DoubleClickAddsToRenameList = true;
                        return Task.FromResult<bool?>(false);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.False(saved);
            Assert.True(session.FileList.RememberLastFolder);
            Assert.True(session.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.Fewer, ConfigStore.Config.Ui.ConfirmationPrompts);
            Assert.False(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies Options is a no-op when the window has no live session.
        /// </summary>
        [AvaloniaFact]
        public async Task ShowOptions_without_session_is_noop()
        {
            var shown = false;
            var (viewModel, window) = _ShowMainWindow(
                session: null,
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
            Assert.Null(viewModel.Session);

            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        private static (MainWindowViewModel ViewModel, AppMainWindow Window) _ShowMainWindow(
            SessionState? session,
            OptionsDialogHooks hooks
        )
        {
            var viewModel = session is null ? new MainWindowViewModel() : new MainWindowViewModel(session: session);
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
