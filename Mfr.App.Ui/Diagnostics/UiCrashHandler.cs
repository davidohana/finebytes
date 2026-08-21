using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;
using Mfr.Engine.Logging;
using Mfr.Models.Config;
using Serilog;

namespace Mfr.App.Ui.Diagnostics
{
    /// <summary>
    /// Process-wide unhandled-exception handling for the Avalonia UI host.
    /// </summary>
    internal static class UiCrashHandler
    {
        private static int _isReporting;

        /// <summary>
        /// When <c>true</c>, faults are logged but the crash dialog is not shown (headless tests).
        /// </summary>
        internal static bool SuppressDialogs { get; set; }

        /// <summary>
        /// Hooks <see cref="AppDomain.UnhandledException"/> and
        /// <see cref="TaskScheduler.UnobservedTaskException"/>. Safe to call more than once.
        /// </summary>
        internal static void RegisterProcessHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException -= _OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += _OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= _OnUnobservedTaskException;
            TaskScheduler.UnobservedTaskException += _OnUnobservedTaskException;
        }

        /// <summary>
        /// Hooks Avalonia dispatcher faults so a UI-thread exception does not kill the process.
        /// </summary>
        internal static void RegisterDispatcherHandler()
        {
            Dispatcher.UIThread.UnhandledException -= _OnDispatcherUnhandledException;
            Dispatcher.UIThread.UnhandledException += _OnDispatcherUnhandledException;
        }

        /// <summary>
        /// Persists a fault to the session log or a best-effort <c>crash-*.log</c> file.
        /// </summary>
        /// <param name="exception">The fault to record.</param>
        /// <param name="isTerminating">Whether the process is shutting down.</param>
        /// <returns>Text and log paths for the crash dialog.</returns>
        internal static CrashReport Persist(Exception exception, bool isTerminating)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var details = LogPaths.FormatCrashText(exception, isTerminating);
            if (LogSession.LogFilePath is { } sessionLogFilePath
                && LogSession.LogDirectoryPath is { } sessionLogDirectoryPath)
            {
                Log.Error(
                    exception,
                    "Unhandled exception. Terminating: {IsTerminating}.",
                    isTerminating);
                if (isTerminating)
                    LogSession.Shutdown();

                return new CrashReport(
                    Details: details,
                    LogFilePath: sessionLogFilePath,
                    LogDirectoryPath: sessionLogDirectoryPath);
            }

            var logDirectoryPath = LogPaths.ResolveDirectoryPath(ConfigLoader.Settings.Log.DirectoryPath);
            var crashFilePath = LogPaths.TryWriteCrashFile(
                logDirectoryPath: logDirectoryPath,
                exception: exception,
                isTerminating: isTerminating);
            return new CrashReport(
                Details: details,
                LogFilePath: crashFilePath,
                LogDirectoryPath: logDirectoryPath);
        }

        /// <summary>
        /// Records a fault and shows the crash dialog when the dispatcher is available.
        /// </summary>
        /// <param name="exception">The fault to report.</param>
        /// <param name="isTerminating">Whether the process will exit after this report.</param>
        internal static void Report(Exception exception, bool isTerminating)
        {
            if (Interlocked.Exchange(ref _isReporting, 1) == 1)
                return;

            try
            {
                var report = Persist(exception, isTerminating);
                if (!SuppressDialogs)
                    _ShowCrashDialog(report, isTerminating);
            }
            catch (Exception)
            {
                // Crash reporting must not throw; the original fault is already in flight.
            }
            finally
            {
                if (!isTerminating)
                    Interlocked.Exchange(ref _isReporting, 0);
            }
        }

        private static void _OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception
                ?? new Exception($"Non-exception unhandled object: {args.ExceptionObject}");
            Report(exception, isTerminating: args.IsTerminating);
        }

        private static void _OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            Report(args.Exception, isTerminating: false);
            args.SetObserved();
        }

        private static void _OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs args)
        {
            args.Handled = true;
            Report(args.Exception, isTerminating: false);
        }

        private static void _ShowCrashDialog(CrashReport report, bool isTerminating)
        {
            if (Application.Current is null)
                return;

            _RunSynchronouslyOnUiThread(async () =>
            {
                var viewModel = new CrashDialogViewModel(
                    details: report.Details,
                    logFilePath: report.LogFilePath,
                    logDirectoryPath: report.LogDirectoryPath,
                    isTerminating: isTerminating);
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
                return desktop.MainWindow;

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
                TaskScheduler.Default);
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
    internal readonly record struct CrashReport(
        string Details,
        string? LogFilePath,
        string LogDirectoryPath);
}
