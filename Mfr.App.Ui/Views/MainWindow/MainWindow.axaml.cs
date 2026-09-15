using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.LogDialog;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.Options;
using Mfr.App.Ui.Views.LogDialog;
using Mfr.App.Ui.Views.Options;
using Mfr.Engine.Config;
using Mfr.Engine.RenameLog;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views.MainWindow
{
    /// <summary>
    /// Main application window with the MFR 7.4 splitter layout.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _boundViewModel;
        private bool _resetConfigurationInProgress;
        private bool _optionsDialogInProgress;
        private bool _logDialogInProgress;

        /// <summary>
        /// Initializes the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += _OnClosing;
            DataContextChanged += _OnDataContextChanged;
        }

        /// <summary>
        /// Optional Reset Configuration overrides for headless tests; leave null in production.
        /// </summary>
        internal ResetConfigurationHooks? ResetConfigurationHooks { get; set; }

        /// <summary>
        /// Optional Options dialog overrides for headless tests; leave null in production.
        /// </summary>
        internal OptionsDialogHooks? OptionsDialogHooks { get; set; }

        /// <summary>
        /// Optional Rename Log dialog overrides for headless tests; leave null in production.
        /// </summary>
        internal RenameLogDialogHooks? RenameLogDialogHooks { get; set; }

        /// <summary>
        /// Builds the pane-grid handle used by session splitter restore/capture.
        /// </summary>
        /// <returns>Named grids for <see cref="SplitterSession"/>.</returns>
        internal MainWindowPaneGrids GetPaneGrids()
        {
            return new MainWindowPaneGrids
            {
                TopPanes = TopPanesGrid,
                FilterLists = FilterListsGrid,
                FilterPanes = FilterPanesGrid,
                MainPanes = MainPanesGrid,
            };
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_boundViewModel is not null)
            {
                _boundViewModel.FilterChainViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;
                _boundViewModel.FilterChainViewModel.FilterHelpMissing -= _OnFilterHelpMissing;
                _boundViewModel.OptionsRequested -= _OnOptionsRequested;
                _boundViewModel.LogRequested -= _OnLogRequested;
                _boundViewModel.ResetConfigurationRequested -= _OnResetConfigurationRequested;
                _boundViewModel = null;
            }

            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            _boundViewModel = viewModel;
            viewModel.FilterChainViewModel.FilterDefaultSaved += _OnFilterDefaultSaved;
            viewModel.FilterChainViewModel.FilterHelpMissing += _OnFilterHelpMissing;
            viewModel.OptionsRequested += _OnOptionsRequested;
            viewModel.LogRequested += _OnLogRequested;
            viewModel.ResetConfigurationRequested += _OnResetConfigurationRequested;
        }

        private void _OnFilterDefaultSaved(object? sender, string catalogDisplayName)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowFilterDefaultSavedAsync(catalogDisplayName));
        }

        private void _OnFilterHelpMissing(object? sender, string helpFileName)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowFilterHelpMissingAsync(helpFileName));
        }

        private void _OnOptionsRequested(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowOptionsAsync());
        }

        private void _OnLogRequested(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowLogAsync());
        }

        private void _OnResetConfigurationRequested(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => _ = _ResetConfigurationAsync());
        }

        /// <summary>
        /// Hosts the Options dialog; on OK commits drafts and writes <c>config.json</c>.
        /// <para>
        /// No-ops when <see cref="MainWindowViewModel.PersistSession"/> is false — remember flags must
        /// land on live <see cref="ConfigStore"/> sections, not a throwaway draft.
        /// </para>
        /// </summary>
        private async Task _ShowOptionsAsync()
        {
            if (_optionsDialogInProgress)
            {
                return;
            }

            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            if (!viewModel.PersistSession)
            {
                return;
            }

            _optionsDialogInProgress = true;
            try
            {
                var dialogVm = new OptionsDialogViewModel();
                var hooks = OptionsDialogHooks;
                bool? accepted;
                if (hooks?.Show is not null)
                {
                    accepted = await hooks.Show(dialogVm);
                }
                else
                {
                    var dialog = new OptionsDialog(dialogVm);
                    accepted = await dialog.ShowDialog<bool?>(this);
                }

                if (accepted != true)
                {
                    return;
                }

                dialogVm.Commit();

                // Production: trim default rename-logs dir. Hooks: only when an explicit path is set
                // (headless OK tests must not prune the developer's AppData logs).
                var pruneDirectoryPath = hooks is null
                    ? RenameLogStore.DefaultDirectoryPath
                    : hooks.PruneRenameLogDirectoryPath;
                if (pruneDirectoryPath is not null)
                {
                    RenameLogStore.PruneFiles(pruneDirectoryPath, ConfigStore.RenameLog.Limit);
                }

                viewModel.RenameListViewModel.NotifyAddPolicyChanged();
                try
                {
                    ConfigStoreSave.Invoke(hooks?.SaveConfig);
                }
                catch (Exception)
                {
                    await new OkMessageDialog(title: "Options", message: "Failed to save configuration.").ShowDialog(
                        this
                    );
                }
            }
            finally
            {
                _optionsDialogInProgress = false;
            }
        }

        /// <summary>
        /// Hosts the Rename Log dialog; on Undo closes then runs the same undo path as Undo Last.
        /// </summary>
        private async Task _ShowLogAsync()
        {
            if (_logDialogInProgress)
            {
                return;
            }

            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            _logDialogInProgress = true;
            try
            {
                var dialogVm = new RenameLogDialogViewModel();
                var hooks = RenameLogDialogHooks;
                bool? accepted;
                if (hooks?.Show is not null)
                {
                    accepted = await hooks.Show(dialogVm);
                }
                else
                {
                    var dialog = new RenameLogDialog(dialogVm);
                    accepted = await dialog.ShowDialog<bool?>(this);
                }

                if (accepted != true || dialogVm.LogToUndo is null)
                {
                    return;
                }

                await viewModel.UndoFromLogAsync(dialogVm.LogToUndo).ConfigureAwait(true);
            }
            finally
            {
                _logDialogInProgress = false;
            }
        }

        private async Task _ShowFilterDefaultSavedAsync(string catalogDisplayName)
        {
            var dialog = new OkMessageDialog(
                title: "Magic File Renamer",
                message: $"Filter settings for the filter '{catalogDisplayName}' saved as default."
            );
            await dialog.ShowDialog(this);
        }

        private async Task _ShowFilterHelpMissingAsync(string helpFileName)
        {
            var dialog = new OkMessageDialog(
                title: "Help",
                message: FilterHelpHost.FormatMissingHelpMessage(helpFileName)
            );
            await dialog.ShowDialog(this);
        }

        /// <summary>
        /// Confirms Reset Configuration, deletes AppData files, skips session save, and restarts.
        /// </summary>
        private async Task _ResetConfigurationAsync()
        {
            if (_resetConfigurationInProgress)
            {
                return;
            }

            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            _resetConfigurationInProgress = true;
            try
            {
                var hooks = ResetConfigurationHooks;
                bool accepted;
                if (hooks?.Confirm is not null)
                {
                    accepted = await hooks.Confirm();
                }
                else
                {
                    var confirm = new ConfirmMessageDialog(
                        title: "Confirmation",
                        message: "Reset configuration to default values? Magic File Renamer will close and restart."
                    );
                    accepted = await confirm.ShowDialog<bool>(this);
                }

                if (!accepted)
                {
                    return;
                }

                try
                {
                    if (hooks?.DeletePersistedConfiguration is not null)
                    {
                        hooks.DeletePersistedConfiguration();
                    }
                    else
                    {
                        PersistedConfigurationReset.Reset();
                    }
                }
                catch (Exception)
                {
                    await new OkMessageDialog(
                        title: "Magic File Renamer",
                        message: "Failed to reset configuration. Error Deleting configuration file."
                    ).ShowDialog(this);
                    return;
                }

                // Prefs file is gone; never write it back from this process, then always exit
                // (restart is best-effort — user can relaunch if spawn fails).
                viewModel.SuppressSessionSaveOnClose = true;

                var restarted = false;
                var exePath = hooks?.ResolveExecutablePath is not null
                    ? hooks.ResolveExecutablePath()
                    : _ResolveExecutablePath();
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    try
                    {
                        if (hooks?.StartProcess is not null)
                        {
                            hooks.StartProcess(exePath);
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
                        }

                        restarted = true;
                    }
                    catch (Exception)
                    {
                        // Notify below, then still exit.
                    }
                }

                if (!restarted)
                {
                    if (hooks?.NotifyRestartFailed is not null)
                    {
                        await hooks.NotifyRestartFailed();
                    }
                    else
                    {
                        await new OkMessageDialog(
                            title: "Magic File Renamer",
                            message: "Failed to restart MFR program file."
                        ).ShowDialog(this);
                    }
                }

                if (hooks?.Shutdown is not null)
                {
                    hooks.Shutdown();
                    return;
                }

                viewModel.Exit();
            }
            finally
            {
                _resetConfigurationInProgress = false;
            }
        }

        /// <summary>
        /// Resolves the running executable path for spawning a replacement process.
        /// </summary>
        /// <returns>Absolute path, or <see langword="null"/> when it cannot be determined.</returns>
        private static string? _ResolveExecutablePath()
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            {
                return Environment.ProcessPath;
            }

            try
            {
                return Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private void _OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            viewModel.FilterChainViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;
            viewModel.FilterChainViewModel.FilterHelpMissing -= _OnFilterHelpMissing;
            viewModel.OptionsRequested -= _OnOptionsRequested;
            viewModel.LogRequested -= _OnLogRequested;
            viewModel.ResetConfigurationRequested -= _OnResetConfigurationRequested;

            if (viewModel.SuppressSessionSaveOnClose)
            {
                return;
            }

            if (!viewModel.PersistSession)
            {
                return;
            }

            UiSessionPersistence.SaveOnClose(
                this,
                GetPaneGrids(),
                viewModel.FileListViewModel.CaptureSession(),
                viewModel.RenameListViewModel.CaptureSession()
            );
        }

        private async void _OnFilterOptionsMenuClick(object? sender, RoutedEventArgs e)
        {
            await FilterChainPane.ShowFilterOptionsAsync();
        }

        private async void _OnPresetsMenuClick(object? sender, RoutedEventArgs e)
        {
            await FilterChainPane.ShowPresetManagerAsync();
        }

        private async void _OnSavePresetMenuClick(object? sender, RoutedEventArgs e)
        {
            await FilterChainPane.ShowSavePresetAsync();
        }
    }
}
