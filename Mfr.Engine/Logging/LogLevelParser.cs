using Mfr.Utils;
using Serilog.Events;

namespace Mfr.Engine.Logging
{
    /// <summary>
    /// Maps host log-level names (<c>debug|info|warn|error</c>) to Serilog levels.
    /// </summary>
    public static class LogLevelParser
    {
        /// <summary>
        /// Default level name when the option is omitted or blank (<c>info</c>).
        /// </summary>
        public const string DefaultName = "info";

        /// <summary>
        /// Maps a level name to a Serilog <see cref="LogEventLevel"/>.
        /// </summary>
        /// <param name="value">Raw option value; blank uses <see cref="DefaultName"/>.</param>
        /// <returns>The resolved Serilog level.</returns>
        /// <exception cref="UserException">Thrown when the value is not a supported level name.</exception>
        public static LogEventLevel Parse(string? value)
        {
            var normalized = value.IsBlank() ? DefaultName : value.Trim().ToLowerInvariant();

            return normalized switch
            {
                "debug" => LogEventLevel.Debug,
                "info" => LogEventLevel.Information,
                "warn" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                _ => throw new UserException($"Unknown log level '{value}'. Use debug|info|warn|error."),
            };
        }
    }
}
