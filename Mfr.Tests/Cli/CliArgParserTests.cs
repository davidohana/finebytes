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

        [Fact]
        /// <summary>
        /// Verifies that <c>--log-dir</c> is parsed and exposed on CLI options.
        /// </summary>
        public void ParseArgs_Accepts_LogDirectoryPath()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean", "--log-dir", "C:\\logs\\mfr"])!;
            Assert.Equal("C:\\logs\\mfr", options.LogDirectoryPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that source inclusion defaults to files enabled and folders disabled.
        /// </summary>
        public void ParseArgs_Defaults_To_Files_Yes_And_Folders_No()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean"])!;
            Assert.True(options.IncludeFiles);
            Assert.False(options.IncludeFolders);
            Assert.False(options.RecursiveDirectoryFileAdd);
        }

        [Fact]
        /// <summary>
        /// Verifies that source inclusion flags parse yes/no values case-insensitively.
        /// </summary>
        public void ParseArgs_Accepts_Files_And_Folders_YesNo_Values()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music", "-p", "clean", "--files", "NO", "--folders", "YeS"])!;
            Assert.False(options.IncludeFiles);
            Assert.True(options.IncludeFolders);
        }

        [Fact]
        /// <summary>
        /// Verifies that at least one source inclusion type must be enabled.
        /// </summary>
        public void ParseArgs_Rejects_Files_No_And_Folders_No()
        {
            var ex = Assert.Throws<UserException>(() => CliArgParser.ParseArgs(["C:\\Music", "-p", "clean", "--files", "no", "--folders", "no"]));
            Assert.Contains("At least one of --files or --folders must be yes.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that invalid yes/no values return a clear user-facing error.
        /// </summary>
        public void ParseArgs_Rejects_Invalid_Files_Or_Folders_Value()
        {
            var ex = Assert.Throws<UserException>(() => CliArgParser.ParseArgs(["C:\\Music", "-p", "clean", "--files", "maybe"]));
            Assert.Contains("Invalid value for --files", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that multiple positional sources are collected in declared order.
        /// </summary>
        public void ParseArgs_Accepts_Multiple_Positional_Sources()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "C:\\Podcasts", "-p", "clean"])!;
            Assert.Equal(["C:\\Music\\*.mp3", "C:\\Podcasts"], options.Sources);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>--core</c> is accepted.
        /// </summary>
        public void ParseArgs_Accepts_Core_Long_Option()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music\\*.mp3", "-p", "clean", "--core"])!;
            Assert.True(options.ContinueOnRenameError);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>--recursive</c> enables recursive directory-source file expansion.
        /// </summary>
        public void ParseArgs_Accepts_Recursive_Long_Option()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music", "-p", "clean", "--recursive"])!;
            Assert.True(options.RecursiveDirectoryFileAdd);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>-r</c> enables recursive directory-source file expansion.
        /// </summary>
        public void ParseArgs_Accepts_Recursive_Short_Option()
        {
            var options = CliArgParser.ParseArgs(["C:\\Music", "-p", "clean", "-r"])!;
            Assert.True(options.RecursiveDirectoryFileAdd);
        }

    }
}
