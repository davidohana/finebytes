using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests that empty or absent modeled blocks project null semantics and merge rewrites fields in place.
    /// </summary>
    public sealed class EmptyTagBlockProjectionTests
    {
        /// <summary>
        /// Verifies empty modeled blocks yield null semantics.
        /// </summary>
        [Fact]
        public void FromOverlay_WithEmptyBlocks_ProjectsNullSemantics()
        {
            var overlay = new AudioTagOverlay
            {
                Id3v2 = new Id3v2TagData { Version = 4, Frames = [] },
                Xiph = new XiphTagData { Fields = [] },
                Ape = new ApeTagData { Fields = [] },
                RiffInfo = new RiffInfoTagData { Fields = [] },
            };

            var common = SemanticAudioTag.FromOverlay(overlay);

            Assert.Null(common.Title);
            Assert.Null(common.Album);
            Assert.Null(common.Performers);
            Assert.Null(common.Genre);
            Assert.Null(common.Year);
        }

        /// <summary>
        /// Verifies an empty ASF descriptor set is treated as an absent block.
        /// </summary>
        [Fact]
        public void FromOverlay_WithEmptyAsfDescriptors_ProjectsNullSemantics()
        {
            var overlay = new AudioTagOverlay { Asf = new AsfTagData() };

            var common = SemanticAudioTag.FromOverlay(overlay);

            Assert.Null(common.Title);
        }

        /// <summary>
        /// Verifies merge rewrites empty blocks from the semantic projection instead of failing.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_WithEmptyBlocks_RewritesBlocksFromSemantics()
        {
            var overlay = new AudioTagOverlay
            {
                Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                Xiph = new XiphTagData { Fields = [] },
                Ape = new ApeTagData { Fields = [] },
                RiffInfo = new RiffInfoTagData { Fields = [] },
            };
            var merged = SemanticAudioTag.FromOverlay(overlay) with { Title = "Recovered" };

            overlay.MergeSemantic(merged);

            Assert.Equal("Recovered", SemanticAudioTag.FromOverlay(overlay).Title);
            Assert.Contains(overlay.Id3v2.Frames, f => f.FrameId == "TIT2");
            Assert.Contains(overlay.Xiph.Fields, f => f.Key == "TITLE");
        }
    }
}
