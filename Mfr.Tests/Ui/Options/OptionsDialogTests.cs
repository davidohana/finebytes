using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
            var emptyPath = Path.Combine(Path.GetTempPath(), "mfr-test-options-ui-empty-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyPath, """{}""");
            try
            {
                ConfigStore.Load(emptyPath);
            }
            finally
            {
                File.Delete(emptyPath);
            }
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
        /// Verifies the Options dialog constructs and shows the three preference checkboxes.
        /// </summary>
        [AvaloniaFact]
        public void OptionsDialog_shows_three_checkboxes()
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
                Assert.Contains("Confirm before replacing Applied Filters when loading a preset", labels);
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

            _ClickFooterButton(dialog, "OK");
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

            _ClickFooterButton(dialog, "Cancel");
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
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = false;

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
                        vm.ConfirmReplaceAppliedFiltersOnLoad = true;
                        return Task.FromResult<bool?>(true);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.NotNull(shown);
            Assert.True(saved);
            Assert.False(session.FileList!.RememberLastFolder);
            Assert.False(session.MainWindow!.RememberWindowState);
            Assert.True(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);

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
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = false;

            var saved = false;
            var (viewModel, window) = _ShowMainWindow(
                session,
                new OptionsDialogHooks
                {
                    Show = vm =>
                    {
                        vm.RememberLastFolder = false;
                        vm.ConfirmReplaceAppliedFiltersOnLoad = true;
                        return Task.FromResult<bool?>(false);
                    },
                    SaveConfig = () => saved = true,
                }
            );

            await _InvokeShowOptionsAsync(viewModel);

            Assert.False(saved);
            Assert.True(session.FileList!.RememberLastFolder);
            Assert.True(session.MainWindow!.RememberWindowState);
            Assert.False(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);

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
        /// Clicks the Options footer button with the given content.
        /// </summary>
        private static void _ClickFooterButton(OptionsDialog dialog, string content)
        {
            var button = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == content);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }
    }
}
