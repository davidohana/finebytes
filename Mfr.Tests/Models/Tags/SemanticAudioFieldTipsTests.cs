using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="SemanticAudioFieldTips"/>.
    /// </summary>
    public sealed class SemanticAudioFieldTipsTests
    {
        /// <summary>
        /// Verifies every semantic field has a non-empty tip.
        /// </summary>
        [Fact]
        public void For_returns_a_tip_for_every_semantic_field()
        {
            foreach (var field in Enum.GetValues<SemanticAudioField>())
            {
                Assert.False(string.IsNullOrWhiteSpace(SemanticAudioFieldTips.For(field)), field.ToString());
            }

            Assert.Equal(SemanticAudioFieldTips.Title, SemanticAudioFieldTips.For(SemanticAudioField.Title));
            Assert.Equal(SemanticAudioFieldTips.Artist, SemanticAudioFieldTips.For(SemanticAudioField.Performers));
            Assert.Equal(SemanticAudioFieldTips.Asin, SemanticAudioFieldTips.For(SemanticAudioField.AmazonId));
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
