using System.Runtime.CompilerServices;
using Mfr.Engine.Beta;

namespace Mfr.Tests
{
    /// <summary>
    /// Process-wide test host setup that must run before any test touches shared statics.
    /// </summary>
    internal static class TestHostInitializer
    {
        /// <summary>
        /// Stubs the beta network clock offline so Commit / expiry checks never hit HTTPS during tests.
        /// </summary>
        [ModuleInitializer]
        internal static void InitializeBetaExpiryGate()
        {
            BetaExpiryGate.ResetForTests();
        }
    }
}
