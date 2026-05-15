using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests structural equality on <see cref="AudioTagOverlay"/> (blocks only; Phase 4).
    /// </summary>
    public sealed class AudioTagOverlayTagBlockEqualityTests
    {
        /// <summary>
        /// Verifies <see cref="AudioTagOverlay.Equals"/> matches identical native blocks.
        /// </summary>
        [Fact]
        public void Equals_WithIdenticalBlocks_ReturnsTrue()
        {
            var xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2, 3] };
            var a = new AudioTagOverlay { Xiph = xiph };
            var b = new AudioTagOverlay { Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2, 3] } };

            Assert.True(a.Equals(b));
            Assert.True(a.TagBlocksStructurallyEquals(b));
        }

        /// <summary>
        /// Verifies differing blocks are detected even when blobs are close in size.
        /// </summary>
        [Fact]
        public void Equals_DetectsBlockDifferences()
        {
            var a = new AudioTagOverlay { Xiph = new SerializedTagBlob { CanonicalTagBytes = [1] } };
            var b = new AudioTagOverlay { Xiph = new SerializedTagBlob { CanonicalTagBytes = [2] } };

            Assert.False(a.Equals(b));
            Assert.False(a.TagBlocksStructurallyEquals(b));
        }

        /// <summary>
        /// Verifies reference equality short-circuits.
        /// </summary>
        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            var a = new AudioTagOverlay { Xiph = new SerializedTagBlob { CanonicalTagBytes = [9] } };

            Assert.True(a.Equals(a));
        }
    }
}
