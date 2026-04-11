using Mfr.Cli;
using Mfr.Core;
using Mfr.Models;
using Mfr.Models.Filters.Advanced;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Cli
{
    /// <summary>
    /// Tests command-line error handling and user-facing output behavior.
    /// </summary>
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
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();

            try
            {
                Console.SetError(errorWriter);

                var exitCode = CliApp.Run(["-p", "xxx", "--log-dir", logDirectoryPath]);
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
            var logDirectoryPath = dir.CombinePath("logs");
            File.WriteAllText(sourcePath, "x");

            var presetManager = new PresetManager(presetsFilePath);
            presetManager.NameToPreset["counter"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "counter",
                Description = null,
                Filters =
                [
                    new CounterFilter(
                        Enabled: true,
                        Target: new FileNameTarget(FileNamePart.Prefix),
                        Options: new CounterOptions(
                            Start: 1,
                            Step: 1,
                            Width: 3,
                            PadChar: "0",
                            Position: CounterPosition.Replace,
                            Separator: " - ",
                            ResetPerFolder: false))
                ]
            };
            presetManager.SavePresets();

            var exitCode = CliApp.Run(
            [
                sourcePath,
                "--preset", "counter",
                "--presets-file", presetsFilePath,
                "--dry-run",
                "--log-dir", logDirectoryPath
            ]);

            Assert.Equal(CliExitCode.Success, exitCode);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(expectedDestinationPath));
        }
    }
}
