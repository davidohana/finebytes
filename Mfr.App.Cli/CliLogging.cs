using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Mfr.App.Cli
{
    internal static class CliLogging
    {
        /// <summary>
        /// Starts the shared file session log plus the CLI console sink.
        /// </summary>
        /// <param name="logLevel">Minimum level for both sinks.</param>
        internal static void Start(LogEventLevel logLevel)
        {
            LogSession.Start(
                logLevel: logLevel,
                logConfig: ConfigStore.Log,
                blankDirectoryDefault: LogPaths.CliDefaultDirectoryPath,
                configureAdditionalSinks: _AddConsoleSink
            );
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
