using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;
using Mfr.Engine.Logging;
using Serilog;

namespace Mfr.App.Ui.Diagnostics
{
    /// <summary>
    /// Process-wide unhandled-exception handling for the Avalonia UI host.
    /// </summary>
    internal static class UiCrashHandler
    {
        private static int _isReporting;
        private static Action<CrashReport>? _showCrashDialog;

        /// <summary>
        /// Hooks <see cref="AppDomain.UnhandledException"/> and
        /// <see cref="TaskScheduler.UnobservedTaskException"/>. Safe to call more than once.
        /// <para>
        /// Unobserved task faults are logged only; they do not show the crash dialog.
        /// The crash dialog is shown only after <see cref="RegisterDispatcherHandler"/> has run.
        /// </para>
        /// </summary>
        internal static void RegisterProcessHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException -= _OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += _OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= _OnUnobservedTaskException;
            TaskScheduler.UnobservedTaskException += _OnUnobservedTaskException;
        }

        /// <summary>
        /// Hooks Avalonia dispatcher faults and enables the crash dialog plus process exit.
        /// <para>
        /// Call from the desktop entry point after Avalonia setup. Hosts that reuse
        /// <see cref="App"/> without owning process lifetime should omit this.
        /// </para>
        /// </summary>
        internal static void RegisterDispatcherHandler()
        {
            _showCrashDialog = _ShowCrashDialog;
            Dispatcher.UIThread.UnhandledException -= _OnDispatcherUnhandledException;
            Dispatcher.UIThread.UnhandledException += _OnDispatcherUnhandledException;
        }

        /// <summary>
        /// Writes the fault to the session log, or to a <c>crash-*.log</c> in the default log folder.
        /// </summary>
        /// <param name="exception">The fault to record.</param>
        /// <returns>Text and log paths for the crash dialog.</returns>
        internal static CrashReport Persist(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var details = LogPaths.FormatCrashText(exception);
            if (!LogSession.IsStarted)
            {
                var crashFilePath = LogPaths.TryWriteCrashFile(exception);
                return new CrashReport(
                    Details: details,
                    LogFilePath: crashFilePath,
                    LogDirectoryPath: LogPaths.DefaultDirectoryPath
                );
            }

            var sessionLogFilePath = LogSession.LogFilePath!;
            var sessionLogDirectoryPath = LogSession.LogDirectoryPath!;
            Log.Error(exception, "Unhandled exception.");
            LogSession.Shutdown();

            return new CrashReport(
                Details: details,
                LogFilePath: sessionLogFilePath,
                LogDirectoryPath: sessionLogDirectoryPath
            );
        }

        /// <summary>
        /// Records a fatal fault and shows the crash dialog when the dispatcher is available.
        /// </summary>
        /// <param name="exception">The fault to report.</param>
        internal static void Report(Exception exception)
        {
            if (Interlocked.Exchange(ref _isReporting, 1) == 1)
            {
                return;
            }

            try
            {
                var report = Persist(exception);
                _showCrashDialog?.Invoke(report);
            }
            catch (Exception)
            {
                // Crash reporting must not throw; the original fault is already in flight.
            }
        }

        private static void _OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception =
                args.ExceptionObject as Exception
                ?? new Exception($"Non-exception unhandled object: {args.ExceptionObject}");
            Report(exception);
        }

        private static void _OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            try
            {
                Log.Error(args.Exception, "Unobserved task exception.");
            }
            catch (Exception)
            {
                // Logging must not throw; mark observed so the process is not torn down later.
            }

            args.SetObserved();
        }

        private static void _OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            args.Handled = true;
            Report(args.Exception);
            _ShutdownApplication();
        }

        /// <summary>
        /// Exits the desktop lifetime after a fatal UI-thread fault.
        /// </summary>
        private static void _ShutdownApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(1);
                return;
            }

            Environment.Exit(1);
        }

        private static void _ShowCrashDialog(CrashReport report)
        {
            if (Application.Current is null)
            {
                return;
            }

            _RunSynchronouslyOnUiThread(async () =>
            {
                var viewModel = new CrashDialogViewModel(
                    details: report.Details,
                    logFilePath: report.LogFilePath,
                    logDirectoryPath: report.LogDirectoryPath
                );
                var dialog = new CrashDialog(viewModel);
                var owner = _TryGetMainWindow();
                if (owner is not null)
                {
                    await dialog.ShowDialog(owner);
                    return;
                }

                var closed = new TaskCompletionSource();
                dialog.Closed += (_, _) => closed.TrySetResult();
                dialog.Show();
                await closed.Task;
            });
        }

        private static Window? _TryGetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            return null;
        }

        private static void _RunSynchronouslyOnUiThread(Func<Task> action)
        {
            var dispatcher = Dispatcher.UIThread;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.InvokeAsync(action).GetAwaiter().GetResult();
                return;
            }

            var task = action();
            if (task.IsCompleted)
            {
                task.GetAwaiter().GetResult();
                return;
            }

            var frame = new DispatcherFrame();
            task.ContinueWith(
                static (_, state) => ((DispatcherFrame)state!).Continue = false,
                frame,
                TaskScheduler.Default
            );
            dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Paths and formatted text for a persisted unexpected fault.
    /// </summary>
    /// <param name="Details">User-copyable crash text.</param>
    /// <param name="LogFilePath">Session or crash log file, when one was written.</param>
    /// <param name="LogDirectoryPath">Directory that contains diagnostic logs.</param>
    internal readonly record struct CrashReport(string Details, string? LogFilePath, string LogDirectoryPath);
}
