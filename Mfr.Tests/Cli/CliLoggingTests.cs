using Mfr.App.Cli;

using Serilog;

using Serilog.Events;



namespace Mfr.Tests.Cli

{

    /// <summary>

    /// Tests CLI Serilog bootstrap and log-level parsing.

    /// </summary>

    [Collection(SessionLogCollection.Name)]

    public class CliLoggingTests : IDisposable

    {

        private readonly TempDirectoryFixture _tempDirectoryFixture = new();



        /// <summary>

        /// Resets <see cref="ConfigLoader.Settings"/> via <see cref="ConfigLoader.Load()"/> before each test class instance.

        /// </summary>

        public CliLoggingTests()

        {

            ConfigLoader.Load();

        }



        /// <summary>

        /// Restores process-level logging and temporary resources.

        /// </summary>

        public void Dispose()

        {

            LogSession.Shutdown();

            ConfigLoader.Load();

            _tempDirectoryFixture.Dispose();

        }



        [Fact]

        /// <summary>

        /// Verifies startup creates a session file under <see cref="LogSettings.DirectoryPath"/>.

        /// </summary>

        public void Start_Creates_PerSession_LogFile()

        {

            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();

            ConfigLoader.Settings.Log.DirectoryPath = logDirectoryPath;



            CliLogging.Start(LogEventLevel.Information);

            var logFilePath = LogSession.LogFilePath;

            Log.Information("hello from test");

            LogSession.Shutdown();



            Assert.NotNull(logFilePath);

            Assert.True(File.Exists(logFilePath));

            Assert.Equal(logDirectoryPath, Path.GetDirectoryName(logFilePath));

            var content = File.ReadAllText(logFilePath);

            Assert.Contains("hello from test", content, StringComparison.Ordinal);

        }



        [Fact]

        /// <summary>

        /// Verifies blank input uses the default level name.

        /// </summary>

        public void ParseLogLevel_Defaults_To_Info()

        {

            Assert.Equal(LogEventLevel.Information, CliLogging.ParseLogLevel(null));

            Assert.Equal(LogEventLevel.Information, CliLogging.ParseLogLevel(" "));

        }



        [Fact]

        /// <summary>

        /// Verifies supported level names map to Serilog levels.

        /// </summary>

        public void ParseLogLevel_Accepts_Supported_Names()

        {

            Assert.Equal(LogEventLevel.Debug, CliLogging.ParseLogLevel("debug"));

            Assert.Equal(LogEventLevel.Information, CliLogging.ParseLogLevel("INFO"));

            Assert.Equal(LogEventLevel.Warning, CliLogging.ParseLogLevel("warn"));

            Assert.Equal(LogEventLevel.Error, CliLogging.ParseLogLevel("error"));

        }

    }

}

