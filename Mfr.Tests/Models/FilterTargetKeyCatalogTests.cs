using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for promoted Apply-To key catalogs in <see cref="Mfr.Models"/>.
    /// </summary>
    public sealed class FilterTargetKeyCatalogTests
    {
        /// <summary>
        /// Verifies modeled ID3v2 frame ids are the disjoint union of singleton and multi-instance sets.
        /// </summary>
        [Fact]
        public void Id3v2_modeled_frame_ids_are_singleton_union_multi_instance()
        {
            Assert.Empty(Id3v2ModeledFrame.SingletonFrameIds.Intersect(Id3v2ModeledFrame.MultiInstanceFrameIds));
            Assert.Equal(
                Id3v2ModeledFrame.SingletonFrameIds.Count + Id3v2ModeledFrame.MultiInstanceFrameIds.Count,
                Id3v2ModeledFrame.AllModeledFrameIds.Count
            );
            Assert.Equal(
                Id3v2ModeledFrame
                    .SingletonFrameIds.OrderBy(static id => id, StringComparer.Ordinal)
                    .Concat(Id3v2ModeledFrame.MultiInstanceFrameIds.OrderBy(static id => id, StringComparer.Ordinal)),
                Id3v2ModeledFrame.AllModeledFrameIds
            );
        }

        /// <summary>
        /// Verifies known Xiph keys used by Metadata and Filter Options stay unique and non-empty.
        /// </summary>
        [Fact]
        public void Xiph_known_keys_are_unique_uppercase()
        {
            Assert.NotEmpty(XiphKnownKeys.All);
            Assert.Equal(XiphKnownKeys.All.Count, XiphKnownKeys.All.Distinct(StringComparer.Ordinal).Count());
            Assert.All(XiphKnownKeys.All, key => Assert.Equal(key.ToUpperInvariant(), key));
        }
    }
}
