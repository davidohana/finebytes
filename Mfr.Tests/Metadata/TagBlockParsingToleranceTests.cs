using Mfr.Metadata;
using Mfr.Models.Tags;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests that unparsable native tag blocks degrade to empty projections instead of throwing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Overlays can carry truncated or opaque blobs (partial reads, hand-built test doubles). Projection and semantic merge
    /// share one rehydrate path, so both must tolerate them.
    /// </para>
    /// </remarks>
    public sealed class TagBlockParsingToleranceTests
    {
        private static readonly byte[] s_GarbageBytes = [1, 2, 3];

        /// <summary>
        /// Verifies every unparsable block yields null semantics rather than propagating a TagLib failure.
        /// </summary>
        [Fact]
        public void FromOverlay_WithUnparsableBlocks_ProjectsNullSemantics()
        {
            var overlay = _GarbageBlockOverlay();

            var common = CommonAudioTag.FromOverlay(overlay);

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

            var common = CommonAudioTag.FromOverlay(overlay);

            Assert.Null(common.Title);
        }

        /// <summary>
        /// Verifies merge rebuilds unparsable blocks from the semantic projection instead of failing.
        /// </summary>
        [Fact]
        public void MergeSemanticOntoNativeBlocks_WithUnparsableBlocks_RewritesBlocksFromSemantics()
        {
            var overlay = _GarbageBlockOverlay();
            var merged = CommonAudioTag.FromOverlay(overlay) with { Title = "Recovered" };

            AudioTagPersistence.MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath: null);

            Assert.Equal("Recovered", CommonAudioTag.FromOverlay(overlay).Title);
            Assert.NotEqual(s_GarbageBytes, overlay.Id3v2!.CanonicalTagBytes);
            Assert.NotEqual(s_GarbageBytes, overlay.Xiph!.CanonicalTagBytes);
        }

        private static AudioTagOverlay _GarbageBlockOverlay()
        {
            return new AudioTagOverlay
            {
                Id3v2 = new Id3v2TagData { Version = 4, CanonicalTagBytes = [.. s_GarbageBytes] },
                Xiph = new SerializedTagBlob { CanonicalTagBytes = [.. s_GarbageBytes] },
                Ape = new SerializedTagBlob { CanonicalTagBytes = [.. s_GarbageBytes] },
                RiffInfo = new SerializedTagBlob { CanonicalTagBytes = [.. s_GarbageBytes] },
            };
        }
    }
}
