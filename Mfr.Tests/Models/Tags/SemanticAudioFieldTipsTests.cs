using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="SemanticAudioFieldTips"/>.
    /// </summary>
    public sealed class SemanticAudioFieldTipsTests
    {
        /// <summary>
        /// Verifies only multi-value / role-confused fields return tips.
        /// </summary>
        [Fact]
        public void For_returns_tips_only_for_non_trivial_fields()
        {
            Assert.Equal(SemanticAudioFieldTips.Artist, SemanticAudioFieldTips.For(SemanticAudioField.Performers));
            Assert.Equal(
                SemanticAudioFieldTips.AlbumArtist,
                SemanticAudioFieldTips.For(SemanticAudioField.AlbumArtists)
            );
            Assert.Equal(SemanticAudioFieldTips.Composer, SemanticAudioFieldTips.For(SemanticAudioField.Composers));
            Assert.Equal(SemanticAudioFieldTips.Genre, SemanticAudioFieldTips.For(SemanticAudioField.Genre));

            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.Title));
            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.Year));
            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.MusicBrainzArtistId));
        }

        /// <summary>
        /// Verifies Artist / Album Artist tips distinguish roles and multi-value joining.
        /// </summary>
        [Fact]
        public void For_artist_tips_clarify_role_and_multi_value()
        {
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
