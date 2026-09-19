using Serilog.Events;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests shared host log-level name parsing.
    /// </summary>
    public sealed class LogLevelParserTests
    {
        /// <summary>
        /// Verifies blank input uses the default level name.
        /// </summary>
        [Fact]
        public void Parse_Defaults_To_Info()
        {
            Assert.Equal(LogEventLevel.Information, LogLevelParser.Parse(null));
            Assert.Equal(LogEventLevel.Information, LogLevelParser.Parse(" "));
            Assert.Equal(LogEventLevel.Information, LogLevelParser.Parse(string.Empty));
        }

        /// <summary>
        /// Verifies supported level names map to Serilog levels (case-insensitive).
        /// </summary>
        [Theory]
        [InlineData("debug", LogEventLevel.Debug)]
        [InlineData("INFO", LogEventLevel.Information)]
        [InlineData("warn", LogEventLevel.Warning)]
        [InlineData("error", LogEventLevel.Error)]
        public void Parse_Accepts_Supported_Names(string value, LogEventLevel expected)
        {
            Assert.Equal(expected, LogLevelParser.Parse(value));
        }

        /// <summary>
        /// Verifies unsupported names throw a clear <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Parse_Rejects_Unsupported_Names()
        {
            var ex = Assert.Throws<UserException>(() => LogLevelParser.Parse("trace"));
            Assert.Contains("Unknown log level", ex.Message, StringComparison.Ordinal);
            Assert.Contains("debug|info|warn|error", ex.Message, StringComparison.Ordinal);
        }
    }
}
