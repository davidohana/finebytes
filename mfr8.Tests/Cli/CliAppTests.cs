using Mfr8.Cli;

namespace mfr8.Tests.Cli
{
    /// <summary>
    /// Tests command-line error handling and user-facing output behavior.
    /// </summary>
    public class CliAppTests
    {
        [Fact]
        /// <summary>
        /// Verifies that missing positional <c>sources</c> reports a clear user-facing error.
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
                Assert.Contains("Missing required argument: sources", output, StringComparison.Ordinal);
            }
            finally
            {
                Console.SetError(originalError);
            }
        }
    }
}
