using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for block vs merged-façade equality helpers on <see cref="AudioTagOverlay"/>.
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

        /// <summary>
        /// Verifies façade-only equality ignores detached blocks.
        /// </summary>
        [Fact]
        public void MergedSemanticFacadesEqual_IgnoresBlocks()
        {
            var a = new AudioTagOverlay { Title = "T", Xiph = new SerializedTagBlob { CanonicalTagBytes = [1] } };
            var b = new AudioTagOverlay { Title = "T", Xiph = new SerializedTagBlob { CanonicalTagBytes = [9] } };

            Assert.True(a.MergedSemanticFacadesEqual(b));
            Assert.False(a.TagBlocksStructurallyEquals(b));
            Assert.False(a.Equals(b));
        }

        /// <summary>
        /// Verifies <see cref="AudioTagOverlay.Equals"/> matches both helpers together.
        /// </summary>
        [Fact]
        public void Equals_RequiresBlocksAndFacades()
        {
            var a = new AudioTagOverlay { Album = "X" };
            var b = new AudioTagOverlay { Album = "Y" };

            Assert.True(a.TagBlocksStructurallyEquals(b));
            Assert.False(a.MergedSemanticFacadesEqual(b));
            Assert.False(a.Equals(b));
        }
    }
}
