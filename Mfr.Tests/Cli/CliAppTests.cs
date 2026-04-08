using Mfr.Cli;
using Mfr.Tests.TestSupport;

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
        /// Verifies that missing positional <c>sources</c> reports a clear user-facing error.
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
                Assert.Contains("Missing required argument: sources", output, StringComparison.Ordinal);
            }
            finally
            {
                Console.SetError(originalError);
            }
        }
    }
}
