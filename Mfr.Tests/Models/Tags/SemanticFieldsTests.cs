using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests <see cref="SemanticFields"/> numeric clear rules and filter/preview string IO.
    /// </summary>
    public sealed class SemanticFieldsTests
    {
        /// <summary>
        /// Verifies writing <c>0</c> for numeric semantic fields clears to null (never stores zero).
        /// </summary>
        [Theory]
        [InlineData(SemanticAudioField.Year)]
        [InlineData(SemanticAudioField.Track)]
        [InlineData(SemanticAudioField.TrackCount)]
        [InlineData(SemanticAudioField.Disc)]
        [InlineData(SemanticAudioField.DiscCount)]
        [InlineData(SemanticAudioField.BeatsPerMinute)]
        public void SetSemanticField_Zero_ClearsNumeric(SemanticAudioField field)
        {
            var overlay = AudioTagOverlayTestBuilder.Id3Overlay(
                year: 1999,
                track: 7,
                trackCount: 12,
                disc: 2,
                discCount: 3,
                beatsPerMinute: 120
            );

            SemanticFields.SetSemanticField(overlay, field, "0");

            var semantic = SemanticAudioTag.FromOverlay(overlay);
            var value = field switch
            {
                SemanticAudioField.Year => semantic.Year,
                SemanticAudioField.Track => semantic.Track,
                SemanticAudioField.TrackCount => semantic.TrackCount,
                SemanticAudioField.Disc => semantic.Disc,
                SemanticAudioField.DiscCount => semantic.DiscCount,
                SemanticAudioField.BeatsPerMinute => semantic.BeatsPerMinute,
                SemanticAudioField.Title => throw new NotImplementedException(),
                SemanticAudioField.Album => throw new NotImplementedException(),
                SemanticAudioField.Performers => throw new NotImplementedException(),
                SemanticAudioField.AlbumArtists => throw new NotImplementedException(),
                SemanticAudioField.Composers => throw new NotImplementedException(),
                SemanticAudioField.Genre => throw new NotImplementedException(),
                SemanticAudioField.Comment => throw new NotImplementedException(),
                SemanticAudioField.Lyrics => throw new NotImplementedException(),
                SemanticAudioField.Copyright => throw new NotImplementedException(),
                SemanticAudioField.Grouping => throw new NotImplementedException(),
                SemanticAudioField.Conductor => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzArtistId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseArtistId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzTrackId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzDiscId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseStatus => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseType => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseCountry => throw new NotImplementedException(),
                SemanticAudioField.MusicIpId => throw new NotImplementedException(),
                SemanticAudioField.AmazonId => throw new NotImplementedException(),
                _ => throw new InvalidOperationException(field.ToString()),
            };

            Assert.Null(value);
            Assert.Equal(string.Empty, SemanticFields.Format(semantic, field));
        }
    }
}
