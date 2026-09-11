using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;
using Mfr.Engine.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Main application window with the MFR 7.4 splitter layout.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _boundViewModel;
        private bool _resetConfigurationInProgress;

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
                _boundViewModel.AppliedFiltersViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;
                _boundViewModel.ResetConfigurationRequested -= _OnResetConfigurationRequested;
                _boundViewModel = null;
            }

            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            _boundViewModel = viewModel;
            viewModel.AppliedFiltersViewModel.FilterDefaultSaved += _OnFilterDefaultSaved;
            viewModel.ResetConfigurationRequested += _OnResetConfigurationRequested;
        }

        private void _OnFilterDefaultSaved(object? sender, string catalogDisplayName)
        {
            Dispatcher.UIThread.Post(() => _ = _ShowFilterDefaultSavedAsync(catalogDisplayName));
        }

        private void _OnResetConfigurationRequested(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => _ = _ResetConfigurationAsync());
        }

        private async Task _ShowFilterDefaultSavedAsync(string catalogDisplayName)
        {
            var dialog = new OkMessageDialog(
                title: "Magic File Renamer",
                message: $"Filter settings for the filter '{catalogDisplayName}' saved as default."
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
                        PersistedConfigurationReset.DeleteAppDataFiles();
                        viewModel.AppliedFiltersViewModel.ClearFilterDefaultsCache();
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

                viewModel.SuppressSessionSaveOnClose = true;

                var exePath = hooks?.ResolveExecutablePath?.Invoke() ?? _ResolveExecutablePath();
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    await new OkMessageDialog(
                        title: "Magic File Renamer",
                        message: "Failed to restart MFR program file."
                    ).ShowDialog(this);
                    return;
                }

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
                }
                catch (Exception)
                {
                    await new OkMessageDialog(
                        title: "Magic File Renamer",
                        message: "Failed to restart MFR program file."
                    ).ShowDialog(this);
                    return;
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

            viewModel.AppliedFiltersViewModel.FilterDefaultSaved -= _OnFilterDefaultSaved;
            viewModel.ResetConfigurationRequested -= _OnResetConfigurationRequested;

            if (viewModel.SuppressSessionSaveOnClose)
            {
                return;
            }

            var session = viewModel.Session;
            if (session is null)
            {
                return;
            }

            UiSessionPersistence.SaveOnClose(
                this,
                GetPaneGrids(),
                session,
                viewModel.FileListViewModel.CaptureSession(),
                viewModel.RenameListViewModel.CaptureSession()
            );
        }

        private async void _OnFilterOptionsMenuClick(object? sender, RoutedEventArgs e)
        {
            await AppliedFiltersPane.ShowFilterOptionsAsync();
        }

        private void _OnSavePresetMenuClick(object? sender, RoutedEventArgs e)
        {
            AppliedFiltersPane.SavePreset();
        }

        private async void _OnSavePresetAsMenuClick(object? sender, RoutedEventArgs e)
        {
            await AppliedFiltersPane.ShowSavePresetAsAsync();
        }
    }
}
