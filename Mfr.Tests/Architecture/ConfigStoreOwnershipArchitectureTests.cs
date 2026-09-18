// Prefs store ownership: docs/plans/layering-ownership-cleanup.plan.md (P1); layering doc in P2

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies process-wide prefs I/O lives in Engine, not Models.
    /// </summary>
    public sealed class ConfigStoreOwnershipArchitectureTests
    {
        /// <summary>
        /// <c>ConfigStore</c> and <c>ConfirmationPolicy</c> must be in assembly <c>Mfr.Engine</c>,
        /// namespace <c>Mfr.Engine.Config</c>. Prefs DTOs (<c>OptionsConfig</c>, <c>ConfirmationKind</c>)
        /// stay in <c>Mfr.Models.Config</c>.
        /// </summary>
        [Fact]
        public void ConfigStore_And_ConfirmationPolicy_Live_In_Engine()
        {
            Assert.Equal("Mfr.Engine", typeof(ConfigStore).Assembly.GetName().Name);
            Assert.Equal("Mfr.Engine.Config", typeof(ConfigStore).Namespace);

            Assert.Equal("Mfr.Engine", typeof(ConfirmationPolicy).Assembly.GetName().Name);
            Assert.Equal("Mfr.Engine.Config", typeof(ConfirmationPolicy).Namespace);

            Assert.Equal("Mfr.Models", typeof(OptionsConfig).Assembly.GetName().Name);
            Assert.Equal("Mfr.Models.Config", typeof(OptionsConfig).Namespace);

            Assert.Equal("Mfr.Models", typeof(ConfirmationKind).Assembly.GetName().Name);
            Assert.Equal("Mfr.Models.Config", typeof(ConfirmationKind).Namespace);
        }

        /// <summary>
        /// Models must not reintroduce a prefs store type under <c>Mfr.Models.Config</c>.
        /// </summary>
        [Fact]
        public void Models_Config_Has_No_ConfigStore_Type()
        {
            var modelsConfigTypes = typeof(OptionsConfig)
                .Assembly.GetTypes()
                .Where(t => t.Namespace == "Mfr.Models.Config")
                .Select(t => t.Name)
                .ToHashSet(StringComparer.Ordinal);

            Assert.DoesNotContain("ConfigStore", modelsConfigTypes);
            Assert.DoesNotContain("ConfirmationPolicy", modelsConfigTypes);
        }
    }
}
