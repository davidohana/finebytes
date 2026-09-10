using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests for <see cref="SemanticAudioFieldLabels"/>.
    /// </summary>
    public sealed class SemanticAudioFieldLabelsTests
    {
        /// <summary>
        /// Verifies every semantic field has a non-empty shared label.
        /// </summary>
        [Fact]
        public void For_covers_every_semantic_field()
        {
            foreach (var field in Enum.GetValues<SemanticAudioField>())
            {
                Assert.False(string.IsNullOrWhiteSpace(SemanticAudioFieldLabels.For(field)));
            }
        }

        /// <summary>
        /// Verifies common fields use the aligned player-facing vocabulary.
        /// </summary>
        [Fact]
        public void For_common_fields_use_aligned_labels()
        {
            Assert.Equal("Artist", SemanticAudioFieldLabels.For(SemanticAudioField.Performers));
            Assert.Equal("Album Artist", SemanticAudioFieldLabels.For(SemanticAudioField.AlbumArtists));
            Assert.Equal("Composer", SemanticAudioFieldLabels.For(SemanticAudioField.Composers));
            Assert.Equal("Genre", SemanticAudioFieldLabels.For(SemanticAudioField.Genre));
            Assert.Equal("BPM", SemanticAudioFieldLabels.For(SemanticAudioField.BeatsPerMinute));
        }

        /// <summary>
        /// Verifies catalog fields reuse Picard TXXX descriptions.
        /// </summary>
        [Fact]
        public void For_catalog_fields_match_picard_txxx_descriptions()
        {
            foreach (var row in AudioCatalogFieldMaps.All)
            {
                Assert.Equal(row.Id3v2TxxxDescription, SemanticAudioFieldLabels.For(row.Field));
            }
        }

        /// <summary>
        /// Verifies first-segment labels append <c>(first)</c>.
        /// </summary>
        [Fact]
        public void FirstSegment_appends_first_suffix()
        {
            Assert.Equal("Artist (first)", SemanticAudioFieldLabels.FirstSegment(SemanticAudioField.Performers));
            Assert.Equal(
                "Album Artist (first)",
                SemanticAudioFieldLabels.FirstSegment(SemanticAudioField.AlbumArtists)
            );
        }
    }
}
