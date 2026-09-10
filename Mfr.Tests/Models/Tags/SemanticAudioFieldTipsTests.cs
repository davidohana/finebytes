using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="SemanticAudioFieldTips"/>.
    /// </summary>
    public sealed class SemanticAudioFieldTipsTests
    {
        /// <summary>
        /// Verifies only ambiguous labels (multi-value, role, or abbreviation) return tips.
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
            Assert.Equal(SemanticAudioFieldTips.Asin, SemanticAudioFieldTips.For(SemanticAudioField.AmazonId));
            Assert.Equal(SemanticAudioFieldTips.Bpm, SemanticAudioFieldTips.For(SemanticAudioField.BeatsPerMinute));

            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.Title));
            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.Year));
            Assert.Null(SemanticAudioFieldTips.For(SemanticAudioField.MusicBrainzArtistId));
        }

        /// <summary>
        /// Verifies first-segment tips name the parent field and original-only behavior.
        /// </summary>
        [Fact]
        public void FirstSegment_clarifies_parent_and_original_only()
        {
            Assert.Equal(
                "Only the first Artist value before `;`. Same tag as Artist; original-only column.",
                SemanticAudioFieldTips.FirstSegment(SemanticAudioField.Performers)
            );
        }
    }
}
