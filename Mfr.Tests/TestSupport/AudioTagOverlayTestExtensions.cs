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
        /// Projects <paramref name="overlay"/> through <see cref="CommonAudioTag.FromOverlay"/>.
        /// </summary>
        public static CommonAudioTag Semantic(this AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            return CommonAudioTag.FromOverlay(overlay);
        }
    }
}
