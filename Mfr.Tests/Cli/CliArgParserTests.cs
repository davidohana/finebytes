using Mfr.Cli;
using Mfr.Core;
using Serilog.Events;

namespace Mfr.Tests.Cli
{
    /// <summary>
    /// Tests command-line parsing behavior in <see cref="CliArgParser"/>.
    /// </summary>
    public class CliArgParserTests
    {
        [Fact]
        /// <summary>
        /// Verifies that omitted <c>--log-level</c> defaults to <c>info</c>.
        /// </summary>
        public void ParseArgs_Defaults_LogLevel_To_Info()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean"])!;
            Assert.Equal(LogEventLevel.Information, options.LogLevel);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>--log-level</c> accepts supported values regardless of letter case.
        /// </summary>
        public void ParseArgs_Accepts_LogLevel_CaseInsensitive()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean", "--log-level", "WARN"])!;
            Assert.Equal(LogEventLevel.Warning, options.LogLevel);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>--log-level debug</c> is accepted.
        /// </summary>
        public void ParseArgs_Accepts_LogLevel_Debug()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean", "--log-level", "debug"])!;
            Assert.Equal(LogEventLevel.Debug, options.LogLevel);
        }

        [Fact]
        /// <summary>
        /// Verifies that unsupported <c>--log-level</c> values return a clear user-facing error.
        /// </summary>
        public void ParseArgs_Rejects_Unsupported_LogLevel()
        {
            var ex = Assert.Throws<UserException>(() => CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean", "--log-level", "trace"]));
            Assert.Contains("Unknown log level", ex.Message, StringComparison.Ordinal);
        }
    }
}
