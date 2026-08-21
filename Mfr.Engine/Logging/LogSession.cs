using Mfr.Models.Config;
using Serilog;
using Serilog.Events;

namespace Mfr.Engine.Logging
{
    /// <summary>
    /// Starts a per-process Serilog file session shared by CLI and UI hosts.
    /// </summary>
    public static class LogSession
    {
        /// <summary>
        /// Gets the current session log file path, or <c>null</c> when logging is not started.
        /// </summary>
        public static string? LogFilePath { get; private set; }

        /// <summary>
        /// Gets the directory containing <see cref="LogFilePath"/>, or <c>null</c> when logging is not started.
        /// </summary>
        public static string? LogDirectoryPath { get; private set; }

        /// <summary>
        /// Creates a session log file, assigns <see cref="Log.Logger"/>, and prunes old sessions.
        /// <para>
        /// Call once per process; then <see cref="Shutdown"/> at exit.
        /// Directory comes from <see cref="LogSettings.DirectoryPath"/> (blank uses the default under LocalApplicationData).
        /// </para>
        /// </summary>
        /// <param name="logLevel">Minimum level written to the file (and any host extras).</param>
        /// <param name="logSettings">Directory, file naming, retention, and file output template.</param>
        /// <param name="configureAdditionalSinks">
        /// Optional host extras (CLI console). Invoked after the file target is added.
        /// </param>
        public static void Start(
            LogEventLevel logLevel,
            LogSettings logSettings,
            Action<LoggerConfiguration>? configureAdditionalSinks = null)
        {
            ArgumentNullException.ThrowIfNull(logSettings);

            var (resolvedLogDirectoryPath, logFilePath) = _PrepareSessionPaths(logSettings);
            _AssignProcessLogger(
                logLevel: logLevel,
                logFilePath: logFilePath,
                logSettings: logSettings,
                configureAdditionalSinks: configureAdditionalSinks);
            LogFilePath = logFilePath;
            LogDirectoryPath = resolvedLogDirectoryPath;

            LogPaths.PruneSessionFiles(
                logDirectoryPath: resolvedLogDirectoryPath,
                maxSessionFiles: logSettings.MaxSessionFiles,
                sessionLogPrefix: logSettings.FilePrefix,
                sessionLogExtension: logSettings.FileExtension);

            Log.Debug(
                "Logging initialized. Level: {LogLevel}. File: {LogFilePath}",
                logLevel,
                logFilePath);
        }

        /// <summary>
        /// Flushes and closes the process logger.
        /// <para>
        /// Safe to call more than once. Hosts should call this once at process exit.
        /// </para>
        /// </summary>
        public static void Shutdown()
        {
            Log.CloseAndFlush();
            LogFilePath = null;
            LogDirectoryPath = null;
        }

        /// <summary>
        /// Resolves the log directory, creates it, and builds a new session file path.
        /// </summary>
        private static (string LogDirectoryPath, string LogFilePath) _PrepareSessionPaths(LogSettings logSettings)
        {
            var logDirectoryPath = LogPaths.ResolveDirectoryPath(logSettings.DirectoryPath);
            Directory.CreateDirectory(logDirectoryPath);
            var logFilePath = LogPaths.CreateSessionFilePath(
                logDirectoryPath: logDirectoryPath,
                logSettings: logSettings);
            return (logDirectoryPath, logFilePath);
        }

        /// <summary>
        /// Builds the Serilog logger for this process and assigns <see cref="Log.Logger"/>.
        /// </summary>
        private static void _AssignProcessLogger(
            LogEventLevel logLevel,
            string logFilePath,
            LogSettings logSettings,
            Action<LoggerConfiguration>? configureAdditionalSinks)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.File(
                    path: logFilePath,
                    outputTemplate: logSettings.FileOutputTemplate,
                    rollingInterval: RollingInterval.Infinite,
                    shared: false);

            configureAdditionalSinks?.Invoke(configuration);
            Log.Logger = configuration.CreateLogger();
        }
    }
}
