namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Serializes tests that mutate process-wide <c>BetaExpiryGate</c> state.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class BetaExpiryGateCollection
    {
        /// <summary>
        /// Collection name for <see cref="CollectionAttribute"/>.
        /// </summary>
        public const string Name = "BetaExpiryGate";
    }
}
