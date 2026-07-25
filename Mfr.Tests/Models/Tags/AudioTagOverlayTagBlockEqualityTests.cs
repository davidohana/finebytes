using System.Collections.Immutable;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests structural equality on <see cref="AudioTagOverlay"/> tag blocks.
    /// </summary>
    public sealed class AudioTagOverlayTagBlockEqualityTests
    {
        /// <summary>
        /// Verifies <see cref="AudioTagOverlay.Equals"/> matches identical tag blocks.
        /// </summary>
        [Fact]
        public void Equals_WithIdenticalBlocks_ReturnsTrue()
        {
            var fields = ImmutableArray.Create(new TextFieldRow("TITLE", ["a"]));
            var a = new AudioTagOverlay { Xiph = new XiphTagData { Fields = fields } };
            var b = new AudioTagOverlay { Xiph = new XiphTagData { Fields = [new TextFieldRow("TITLE", ["a"])] } };

            Assert.True(a.Equals(b));
            Assert.True(a.TagBlocksStructurallyEquals(b));
        }

        /// <summary>
        /// Verifies differing blocks are detected even when field counts match.
        /// </summary>
        [Fact]
        public void Equals_DetectsBlockDifferences()
        {
            var a = new AudioTagOverlay { Xiph = new XiphTagData { Fields = [new TextFieldRow("TITLE", ["1"])] } };
            var b = new AudioTagOverlay { Xiph = new XiphTagData { Fields = [new TextFieldRow("TITLE", ["2"])] } };

            Assert.False(a.Equals(b));
            Assert.False(a.TagBlocksStructurallyEquals(b));
        }

        /// <summary>
        /// Verifies reference equality short-circuits.
        /// </summary>
        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            var a = new AudioTagOverlay { Xiph = new XiphTagData { Fields = [new TextFieldRow("TITLE", ["x"])] } };

            Assert.True(a.Equals(a));
        }
    }
}
