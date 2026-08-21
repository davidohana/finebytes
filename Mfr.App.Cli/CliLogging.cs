using Mfr.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Events;

namespace Mfr.App.Cli
{
    internal static class CliLogging
    {
        internal const string DefaultLogLevelName = "info";

        internal static void Start(LogEventLevel logLevel)
        {
            LogSession.Start(
                logLevel: logLevel,
                logSettings: ConfigLoader.Settings.Log,
                configureAdditionalSinks: _AddConsoleSink);
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

        /// <summary>
        /// Adds the CLI console sink (errors to stderr) using <see cref="ConfigLoader.Settings"/>.
        /// </summary>
        /// <param name="configuration">Serilog configuration after the shared file sink is attached.</param>
        private static void _AddConsoleSink(LoggerConfiguration configuration)
        {
            configuration.WriteTo.Console(
                outputTemplate: ConfigLoader.Settings.Log.ConsoleOutputTemplate,
                theme: AnsiConsoleTheme.Code,
                standardErrorFromLevel: LogEventLevel.Error);
        }
    }
}
