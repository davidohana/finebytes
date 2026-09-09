namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Serializes tests that mutate <see cref="ConfigStore.Config"/> (process-wide singleton).
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class ConfigStoreCollection
    {
        /// <summary>
        /// Collection name for ConfigStore tests.
        /// </summary>
        public const string Name = "ConfigStore";
    }
}
