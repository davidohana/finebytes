using Mfr.Engine.Logging;
using Mfr.Models;
using Serilog.Events;

namespace Mfr.App.Ui
{
    /// <summary>
    /// Resolves desktop <c>-l</c>/<c>--log-level</c> before <see cref="LogSession.Start"/> and holds a one-shot soft warning.
    /// </summary>
    internal static class UiLogLevelArgs
    {
        private static string? _pendingSoftWarning;

        /// <summary>
        /// Resolves the UI log level from argv (last <c>-l</c>/<c>--log-level</c> wins).
        /// <para>
        /// Missing flag → Information. Invalid or missing value → Information plus a soft-fail warning string.
        /// </para>
        /// </summary>
        /// <param name="args">Desktop argv (may be empty).</param>
        /// <returns>Resolved Serilog level and an optional soft-fail warning message.</returns>
        internal static (LogEventLevel Level, string? SoftWarning) Resolve(string[] args)
        {
            string? optionName = null;
            string? rawValue = null;
            var missingValue = false;

            for (var i = 0; i < args.Length; i++)
            {
                var token = args[i];
                if (token is not ("-l" or "--log-level"))
                {
                    continue;
                }

                optionName = token;
                var nextIndex = i + 1;
                if (nextIndex >= args.Length || args[nextIndex].StartsWith('-'))
                {
                    rawValue = null;
                    missingValue = true;
                    continue;
                }

                i = nextIndex;
                rawValue = args[i];
                missingValue = false;
            }

            if (optionName is null)
            {
                return (LogEventLevel.Information, null);
            }

            if (missingValue)
            {
                return (LogEventLevel.Information, $"Missing value for {optionName}.");
            }

            try
            {
                return (LogLevelParser.Parse(rawValue), null);
            }
            catch (UserException ex)
            {
                return (LogEventLevel.Information, ex.Message);
            }
        }

        /// <summary>
        /// Resolves the level and stores any soft-fail warning for <see cref="TakeSoftWarning"/>.
        /// </summary>
        /// <param name="args">Desktop argv (may be empty).</param>
        /// <returns>Resolved Serilog level (Information on soft-fail).</returns>
        internal static LogEventLevel ResolveAndRememberWarning(string[] args)
        {
            var (level, softWarning) = Resolve(args);
            _pendingSoftWarning = softWarning;
            return level;
        }

        /// <summary>
        /// Takes and clears the soft-fail warning from the last <see cref="ResolveAndRememberWarning"/> call.
        /// </summary>
        /// <returns>Pending warning text, or <c>null</c> when none.</returns>
        internal static string? TakeSoftWarning()
        {
            var warning = _pendingSoftWarning;
            _pendingSoftWarning = null;
            return warning;
        }
    }
}
