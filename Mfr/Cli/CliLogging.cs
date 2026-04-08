using Mfr.Core;
using Mfr.Utils;
using Serilog;
using Serilog.Events;

namespace Mfr.Cli
{
    internal static class CliLogging
    {
        internal const string DefaultLogLevelName = "info";
        internal const int MaxSessionLogFiles = 100;
        private const string SessionLogPrefix = "session-";
        private const string SessionLogExtension = ".log";
        private const string ConsoleOutputTemplate = "{Message:lj}{NewLine}";
        private const string FileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}";

        internal static CliLoggerSession Start(LogEventLevel logLevel, string? logDirectoryPath)
        {
            var resolvedLogDirectoryPath = _ResolveLogDirectoryPath(logDirectoryPath);
            _ = Directory.CreateDirectory(resolvedLogDirectoryPath);
            _PruneSessionLogFiles(resolvedLogDirectoryPath, MaxSessionLogFiles);

            var fileName = $"{SessionLogPrefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}{SessionLogExtension}";
            var logFilePath = Path.Combine(resolvedLogDirectoryPath, fileName);

            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console(
                    outputTemplate: ConsoleOutputTemplate,
                    standardErrorFromLevel: LogEventLevel.Error)
                .WriteTo.File(
                    path: logFilePath,
                    outputTemplate: FileOutputTemplate,
                    rollingInterval: RollingInterval.Infinite,
                    shared: false)
                .CreateLogger();

            Log.Logger = logger;
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

        private static void _PruneSessionLogFiles(string logDirectoryPath, int maxSessionFiles)
        {
            if (!Directory.Exists(logDirectoryPath))
            {
                return;
            }

            var sessionLogFilePaths = Directory
                .EnumerateFiles(logDirectoryPath, $"{SessionLogPrefix}*{SessionLogExtension}", SearchOption.TopDirectoryOnly)
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
                fileInfo.Delete();
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
