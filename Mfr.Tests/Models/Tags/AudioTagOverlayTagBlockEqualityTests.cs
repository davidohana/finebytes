using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="AudioTagOverlay.TagBlocksStructurallyEquals"/>.
    /// </summary>
    public sealed class AudioTagOverlayTagBlockEqualityTests
    {
        /// <summary>
        /// Verifies block-only equality ignores façade scalars.
        /// </summary>
        [Fact]
        public void TagBlocksStructurallyEquals_IgnoresMergedScalars()
        {
            var a = new AudioTagOverlay
            {
                Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2, 3] },
                Title = "A",
            };

            var b = new AudioTagOverlay
            {
                Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2, 3] },
                Title = "B",
            };

            Assert.True(a.TagBlocksStructurallyEquals(b));
            Assert.False(a.Equals(b));
        }

        /// <summary>
        /// Verifies differing blocks are reported even when façade matches.
        /// </summary>
        [Fact]
        public void TagBlocksStructurallyEquals_DetectsBlockDifferences()
        {
            var a = new AudioTagOverlay
            {
                Xiph = new SerializedTagBlob { CanonicalTagBytes = [1] },
                Title = "Same",
            };

            var b = new AudioTagOverlay
            {
                Xiph = new SerializedTagBlob { CanonicalTagBytes = [2] },
                Title = "Same",
            };

            Assert.False(a.TagBlocksStructurallyEquals(b));
        }
    }
}
