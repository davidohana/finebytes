using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="SemanticAudioFieldTips"/>.
    /// </summary>
    public sealed class SemanticAudioFieldTipsTests
    {
        /// <summary>
        /// Verifies every semantic field has a non-empty tooltip.
        /// </summary>
        [Fact]
        public void For_covers_every_semantic_field()
        {
            foreach (var field in Enum.GetValues<SemanticAudioField>())
            {
                Assert.False(string.IsNullOrWhiteSpace(SemanticAudioFieldTips.For(field)));
            }
        }

        /// <summary>
        /// Verifies Artist / Album Artist tips distinguish roles and multi-value joining.
        /// </summary>
        [Fact]
        public void For_artist_tips_clarify_role_and_multi_value()
        {
            Assert.Equal(SemanticAudioFieldTips.Artist, SemanticAudioFieldTips.For(SemanticAudioField.Performers));
            Assert.Equal(
                SemanticAudioFieldTips.AlbumArtist,
                SemanticAudioFieldTips.For(SemanticAudioField.AlbumArtists)
            );
            Assert.Contains("Track performers", SemanticAudioFieldTips.Artist, StringComparison.Ordinal);
            Assert.Contains("Album-level", SemanticAudioFieldTips.AlbumArtist, StringComparison.Ordinal);
            Assert.Contains("`;`", SemanticAudioFieldTips.Artist, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies first-segment tips name the parent field and original-only behavior.
        /// </summary>
        [Fact]
        public void FirstSegment_clarifies_parent_and_original_only()
        {
            var tip = SemanticAudioFieldTips.FirstSegment(SemanticAudioField.Performers);
            Assert.Contains("Artist", tip, StringComparison.Ordinal);
            Assert.Contains("`;`", tip, StringComparison.Ordinal);
            Assert.Contains("original-only", tip, StringComparison.Ordinal);
        }
    }
}
