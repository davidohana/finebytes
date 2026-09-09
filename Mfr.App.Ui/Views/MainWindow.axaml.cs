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
        /// Optional override for the reset confirmation dialog (tests).
        /// </summary>
        internal Func<Task<bool>>? ConfirmResetForTests { get; set; }

        /// <summary>
        /// Optional override for resolving the executable path (tests).
        /// </summary>
        internal Func<string?>? ResolveExecutablePathForTests { get; set; }

        /// <summary>
        /// Optional override for starting a replacement process (tests).
        /// </summary>
        internal Action<string>? StartProcessForTests { get; set; }

        /// <summary>
        /// Optional override for AppData file deletion (tests).
        /// </summary>
        internal Action? DeletePersistedConfigurationForTests { get; set; }

        /// <summary>
        /// Optional override for shutting down after a successful reset restart (tests).
        /// </summary>
        internal Action? ShutdownForTests { get; set; }

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

        private async Task _ResetConfigurationAsync()
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            bool accepted;
            if (ConfirmResetForTests is not null)
            {
                accepted = await ConfirmResetForTests();
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
                if (DeletePersistedConfigurationForTests is not null)
                {
                    DeletePersistedConfigurationForTests();
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

            var exePath = ResolveExecutablePathForTests?.Invoke() ?? _ResolveExecutablePath();
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
                if (StartProcessForTests is not null)
                {
                    StartProcessForTests(exePath);
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

            if (ShutdownForTests is not null)
            {
                ShutdownForTests();
                return;
            }

            viewModel.Exit();
        }

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
                session,
                viewModel.FileListViewModel.CaptureSession(),
                viewModel.RenameListViewModel.CaptureSession()
            );
        }

        private async void _OnFilterOptionsMenuClick(object? sender, RoutedEventArgs e)
        {
            await AppliedFiltersPane.ShowFilterOptionsAsync();
        }
    }
}
