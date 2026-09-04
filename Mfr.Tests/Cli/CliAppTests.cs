using Mfr.App.Cli;
using Mfr.Filters.Formatting;
using Mfr.Utils;

namespace Mfr.Tests.Cli
{
    /// <summary>
    /// Tests command-line error handling and user-facing output behavior.
    /// </summary>
    [Collection(SessionLogCollection.Name)]
    public class CliAppTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Restores temporary resources used by CLI tests.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies that missing required sources report a clear user-facing error.
        /// </summary>
        public void Shows_Clear_Message_When_Sources_Are_Missing()
        {
            using var errorWriter = new StringWriter();
            var originalError = Console.Error;

            try
            {
                Console.SetError(errorWriter);

                var exitCode = CliApp.Run(["-p", "xxx"]);
                var output = errorWriter.ToString();

                Assert.Equal(CliExitCode.UserError, exitCode);
                Assert.Contains("Missing required argument 'SOURCES'.", output, StringComparison.Ordinal);
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        [Fact]
        /// <summary>
        /// Verifies that dry-run completes successfully without moving files on disk.
        /// </summary>
        public void Run_DryRun_DoesNotMoveFiles_OnDisk()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("track01.mp3");
            var expectedDestinationPath = dir.CombinePath("001.mp3");
            var presetsFilePath = dir.CombinePath("presets.json");
            File.WriteAllText(sourcePath, "x");

            var presetManager = new PresetManager(presetsFilePath);
            presetManager.NameToPreset["counter"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new CounterFilter(
                        Target: new FilePrefixTarget(),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            LeadingZerosMode: CounterLeadingZerosMode.Custom,
                            CustomLength: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false
                        )
                    ),
                ]),
            };
            presetManager.SavePresets();

            var configPath = dir.CombinePath("config.json");
            File.WriteAllText(configPath, """{}""");

            var exitCode = CliApp.Run([
                sourcePath,
                "--preset",
                "counter",
                "--presets-file",
                presetsFilePath,
                "--config",
                configPath,
                "--dry-run",
            ]);

            Assert.Equal(CliExitCode.Success, exitCode);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(expectedDestinationPath));
        }
    }
}
