using Mfr.Models;
using Mfr.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Events;

namespace Mfr.App.Cli
{
    internal static class CliLogging
    {
        internal const string DefaultLogLevelName = "info";

        internal static CliLoggerSession Start(LogEventLevel logLevel, string? logDirectoryPath)
        {
            var logSettings = ConfigLoader.Settings.Log;
            var resolvedLogDirectoryPath = _ResolveLogDirectoryPath(logDirectoryPath);
            Directory.CreateDirectory(resolvedLogDirectoryPath);

            var fileName = $"{logSettings.FilePrefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}{logSettings.FileExtension}";
            var logFilePath = Path.Combine(resolvedLogDirectoryPath, fileName);

            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console(
                    outputTemplate: logSettings.ConsoleOutputTemplate,
                    theme: AnsiConsoleTheme.Code,
                    standardErrorFromLevel: LogEventLevel.Error)
                .WriteTo.File(
                    path: logFilePath,
                    outputTemplate: logSettings.FileOutputTemplate,
                    rollingInterval: RollingInterval.Infinite,
                    shared: false)
                .CreateLogger();

            Log.Logger = logger;
            _PruneSessionLogFiles(
                logDirectoryPath: resolvedLogDirectoryPath,
                maxSessionFiles: logSettings.MaxSessionFiles,
                sessionLogPrefix: logSettings.FilePrefix,
                sessionLogExtension: logSettings.FileExtension);
            logger.Debug(
                "Logging initialized. Level: {LogLevel}. File: {LogFilePath}",
                logLevel,
                logFilePath);
            return new CliLoggerSession(logger, logFilePath);
        }

        internal static LogEventLevel ParseLogLevel(string? value)
        {
            var normalized = value.IsBlank()
                ? DefaultLogLevelName
                : value.Trim().ToLowerInvariant();

            return normalized switch
            {
                "debug" => LogEventLevel.Debug,
                "info" => LogEventLevel.Information,
                "warn" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                _ => throw new UserException($"Unknown log level '{value}'. Use debug|info|warn|error.")
            };
        }

        private static void _PruneSessionLogFiles(
            string logDirectoryPath,
            int maxSessionFiles,
            string sessionLogPrefix,
            string sessionLogExtension)
        {
            if (!Directory.Exists(logDirectoryPath))
            {
                return;
            }

            var sessionLogFilePaths = Directory
                .EnumerateFiles(
                    logDirectoryPath,
                    $"{sessionLogPrefix}*{sessionLogExtension}",
                    SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(fileInfo => fileInfo.CreationTimeUtc)
                .ThenByDescending(fileInfo => fileInfo.Name, StringComparer.Ordinal)
                .ToList();

            if (sessionLogFilePaths.Count <= maxSessionFiles)
            {
                return;
            }

            foreach (var fileInfo in sessionLogFilePaths.Skip(maxSessionFiles))
            {
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        ex,
                        "Failed to delete old log file '{LogFilePath}' during pruning.",
                        fileInfo.FullName);
                }
            }
        }

        private static string _ResolveLogDirectoryPath(string? configuredLogDirectoryPath)
        {
            if (!configuredLogDirectoryPath.IsBlank())
            {
                return configuredLogDirectoryPath.Trim();
            }

            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppDataPath, "finebytes", "mfr", "logs");
        }
    }

    internal sealed class CliLoggerSession : IDisposable
    {
        internal CliLoggerSession(ILogger logger, string logFilePath)
        {
            Logger = logger;
            LogFilePath = logFilePath;
        }

        internal ILogger Logger { get; }

        internal string LogFilePath { get; }

        public void Dispose()
        {
            Log.CloseAndFlush();
        }
    }
}
