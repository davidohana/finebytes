namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Serializes tests that call <c>CliArgParser.ParseArgs</c> (static Spectre parse capture).
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class CliArgParserCollection
    {
        /// <summary>
        /// Collection name for CLI argument-parser tests.
        /// </summary>
        public const string Name = "CliArgParser";
    }
}
