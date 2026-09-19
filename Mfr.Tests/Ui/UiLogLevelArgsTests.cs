using Mfr.App.Ui;
using Serilog.Events;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests desktop <c>-l</c>/<c>--log-level</c> resolution before <see cref="LogSession.Start"/>.
    /// </summary>
    public sealed class UiLogLevelArgsTests
    {
        /// <summary>
        /// Verifies omitted log-level defaults to Information with no warning.
        /// </summary>
        [Fact]
        public void Resolve_Defaults_To_Info_When_Omitted()
        {
            var (level, softWarning) = UiLogLevelArgs.Resolve(["C:\\Music", "--initial-folder", "C:\\photos"]);
            Assert.Equal(LogEventLevel.Information, level);
            Assert.Null(softWarning);
        }

        /// <summary>
        /// Verifies supported names resolve (short and long form; last wins).
        /// </summary>
        [Fact]
        public void Resolve_Accepts_Supported_Levels()
        {
            Assert.Equal(LogEventLevel.Debug, UiLogLevelArgs.Resolve(["--log-level", "debug"]).Level);
            Assert.Equal(LogEventLevel.Warning, UiLogLevelArgs.Resolve(["-l", "WARN"]).Level);
            Assert.Equal(LogEventLevel.Error, UiLogLevelArgs.Resolve(["-l", "info", "--log-level", "error"]).Level);
            Assert.Null(UiLogLevelArgs.Resolve(["--log-level", "debug"]).SoftWarning);
        }

        /// <summary>
        /// Verifies unknown levels soft-fail to Information with a warning string.
        /// </summary>
        [Fact]
        public void Resolve_Soft_Fails_Unknown_Level()
        {
            var (level, softWarning) = UiLogLevelArgs.Resolve(["--log-level", "trace"]);
            Assert.Equal(LogEventLevel.Information, level);
            Assert.NotNull(softWarning);
            Assert.Contains("Unknown log level", softWarning, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies <see cref="UiLogLevelArgs.TakeSoftWarning"/> is one-shot after remember.
        /// </summary>
        [Fact]
        public void ResolveAndRememberWarning_TakeSoftWarning_Is_One_Shot()
        {
            var level = UiLogLevelArgs.ResolveAndRememberWarning(["-l", "verbose"]);
            Assert.Equal(LogEventLevel.Information, level);

            var first = UiLogLevelArgs.TakeSoftWarning();
            Assert.NotNull(first);
            Assert.Contains("Unknown log level", first, StringComparison.Ordinal);
            Assert.Null(UiLogLevelArgs.TakeSoftWarning());
        }
    }
}
