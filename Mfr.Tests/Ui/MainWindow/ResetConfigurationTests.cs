using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Headless tests for Tools → Reset Configuration (without live process restart).
    /// </summary>
    public sealed class ResetConfigurationTests
    {
        /// <summary>
        /// Verifies cancel leaves SuppressSessionSaveOnClose false and does not delete or restart.
        /// </summary>
        [AvaloniaFact]
        public async Task ResetConfiguration_Cancel_Does_Not_Delete_Or_Restart()
        {
            var deleted = false;
            var started = false;
            var shutdown = false;
            var viewModel = new MainWindowViewModel(session: new SessionState());
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
                ResetConfigurationHooks = new ResetConfigurationHooks
                {
                    Confirm = () => Task.FromResult(false),
                    DeletePersistedConfiguration = () => deleted = true,
                    ResolveExecutablePath = () => @"C:\fake\mfr.exe",
                    StartProcess = _ => started = true,
                    Shutdown = () => shutdown = true,
                },
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            viewModel.ResetConfiguration();
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();

            Assert.False(deleted);
            Assert.False(started);
            Assert.False(shutdown);
            Assert.False(viewModel.SuppressSessionSaveOnClose);

            // Closing must not hit real AppData; Cancel leaves SuppressSessionSaveOnClose false.
            viewModel.SuppressSessionSaveOnClose = true;
            window.Close();
        }

        /// <summary>
        /// Verifies OK deletes, suppresses session save, starts the replacement process, and shuts down.
        /// </summary>
        [AvaloniaFact]
        public async Task ResetConfiguration_Ok_Deletes_Suppresses_Save_Restarts_And_Shuts_Down()
        {
            var deleted = false;
            string? startedPath = null;
            var shutdown = false;
            var viewModel = new MainWindowViewModel(session: new SessionState());
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
                ResetConfigurationHooks = new ResetConfigurationHooks
                {
                    Confirm = () => Task.FromResult(true),
                    DeletePersistedConfiguration = () => deleted = true,
                    ResolveExecutablePath = () => @"C:\fake\mfr.exe",
                    StartProcess = path => startedPath = path,
                    Shutdown = () => shutdown = true,
                },
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            viewModel.ResetConfiguration();
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();

            Assert.True(deleted);
            Assert.True(viewModel.SuppressSessionSaveOnClose);
            Assert.Equal(@"C:\fake\mfr.exe", startedPath);
            Assert.True(shutdown);
            window.Close();
        }

        /// <summary>
        /// Verifies closing with SuppressSessionSaveOnClose does not throw when a session is present.
        /// </summary>
        [AvaloniaFact]
        public void SuppressSessionSaveOnClose_Allows_Close_Without_Throw()
        {
            var viewModel = new MainWindowViewModel(session: new SessionState()) { SuppressSessionSaveOnClose = true };
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.True(viewModel.SuppressSessionSaveOnClose);
        }

        /// <summary>
        /// Verifies a second Reset Configuration request while confirm is pending is ignored.
        /// </summary>
        [AvaloniaFact]
        public async Task ResetConfiguration_Ignores_Reentrant_Request()
        {
            var confirmCalls = 0;
            var deleted = 0;
            var tcs = new TaskCompletionSource<bool>();
            var viewModel = new MainWindowViewModel(session: new SessionState());
            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 800,
                Height = 600,
                ResetConfigurationHooks = new ResetConfigurationHooks
                {
                    Confirm = async () =>
                    {
                        confirmCalls++;
                        return await tcs.Task;
                    },
                    DeletePersistedConfiguration = () => deleted++,
                    ResolveExecutablePath = () => @"C:\fake\mfr.exe",
                    StartProcess = _ => { },
                    Shutdown = () => { },
                },
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            viewModel.ResetConfiguration();
            Dispatcher.UIThread.RunJobs();
            viewModel.ResetConfiguration();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, confirmCalls);

            tcs.SetResult(true);
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, confirmCalls);
            Assert.Equal(1, deleted);
            // OK path already sets SuppressSessionSaveOnClose; keep it so Close cannot write AppData.
            Assert.True(viewModel.SuppressSessionSaveOnClose);
            window.Close();
        }
    }
}
