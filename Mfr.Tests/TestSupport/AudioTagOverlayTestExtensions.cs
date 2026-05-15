using Mfr.Metadata;
using Mfr.Models.Tags;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Helpers for reading block-derived semantics in tests.
    /// </summary>
    internal static class AudioTagOverlayTestExtensions
    {
        /// <summary>
        /// Projects <paramref name="overlay"/> through <see cref="AudioTagSemanticSurface.FromBlocks"/>.
        /// </summary>
        public static AudioTagSemanticSurface Semantic(this AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            return AudioTagSemanticSurface.FromBlocks(overlay);
        }
    }
}
