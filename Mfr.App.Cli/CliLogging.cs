using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Mfr.App.Cli
{
    internal static class CliLogging
    {
        internal const string DefaultLogLevelName = LogLevelParser.DefaultName;

        /// <summary>
        /// Starts the shared file session log plus the CLI console sink.
        /// </summary>
        /// <param name="logLevel">Minimum level for both sinks.</param>
        internal static void Start(LogEventLevel logLevel)
        {
            LogSession.Start(logLevel: logLevel, logConfig: ConfigStore.Log, configureAdditionalSinks: _AddConsoleSink);
        }

        /// <summary>
        /// Maps CLI level names (<c>debug|info|warn|error</c>) to Serilog levels.
        /// </summary>
        /// <param name="value">Raw option value; blank uses <see cref="DefaultLogLevelName"/>.</param>
        /// <returns>The resolved Serilog level.</returns>
        /// <exception cref="UserException">Thrown when the value is not a supported level name.</exception>
        internal static LogEventLevel ParseLogLevel(string? value)
        {
            return LogLevelParser.Parse(value);
        }

        /// <summary>
        /// Adds the CLI console sink (errors to stderr) using <see cref="ConfigStore.Log"/>.
        /// </summary>
        /// <param name="configuration">Serilog configuration after the shared file sink is attached.</param>
        private static void _AddConsoleSink(LoggerConfiguration configuration)
        {
            configuration.WriteTo.Console(
                outputTemplate: ConfigStore.Log.ConsoleOutputTemplate,
                theme: AnsiConsoleTheme.Code,
                standardErrorFromLevel: LogEventLevel.Error
            );
        }
    }
}
