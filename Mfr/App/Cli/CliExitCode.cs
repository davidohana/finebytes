namespace Mfr.Cli
{
    /// <summary>
    /// Represents process exit codes returned by the CLI.
    /// </summary>
    public enum CliExitCode
    {
        /// <summary>
        /// The command completed successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The command failed due to invalid user input or configuration.
        /// </summary>
        UserError = 1,

        /// <summary>
        /// The command failed due to an unexpected application fault.
        /// </summary>
        AppError = 2
    }
}
