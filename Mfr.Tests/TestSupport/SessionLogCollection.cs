namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Serializes tests that mutate process-wide Serilog / log-session state.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class SessionLogCollection
    {
        /// <summary>
        /// Collection name for diagnostic-log tests.
        /// </summary>
        public const string Name = "SessionLog";
    }
}
