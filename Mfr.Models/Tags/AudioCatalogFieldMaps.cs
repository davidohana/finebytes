namespace Mfr.Models.Tags
{
    /// <summary>
    /// Picard/TagLib key names for cross-format catalog identifier fields on <see cref="SemanticAudioTag"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ID3v2 stores these as <c>TXXX</c> content descriptors; Xiph/APE use Vorbis-style keys; ASF uses
    /// extended content descriptor names. Apple freeform atoms and RIFF INFO are not mapped here.
    /// </para>
    /// </remarks>
    public static class AudioCatalogFieldMaps
    {
        /// <summary>One catalog field’s native key names across capable blocks.</summary>
        /// <param name="Field">Semantic field this row describes.</param>
        /// <param name="Id3v2TxxxDescription">ID3v2 <c>TXXX</c> content descriptor.</param>
        /// <param name="XiphKey">Xiph/Vorbis comment key.</param>
        /// <param name="ApeKey">APE item key (same spelling as Xiph for these fields).</param>
        /// <param name="AsfDescriptor">ASF extended descriptor name.</param>
        public sealed record CatalogKeyRow(
            SemanticAudioField Field,
            string Id3v2TxxxDescription,
            string XiphKey,
            string ApeKey,
            string AsfDescriptor
        );

        /// <summary>All catalog fields modeled on <see cref="SemanticAudioTag"/>.</summary>
        public static IReadOnlyList<CatalogKeyRow> All { get; } =
        [
            new(
                SemanticAudioField.MusicBrainzArtistId,
                "MusicBrainz Artist Id",
                "MUSICBRAINZ_ARTISTID",
                "MUSICBRAINZ_ARTISTID",
                "MusicBrainz/Artist Id"
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseId,
                "MusicBrainz Album Id",
                "MUSICBRAINZ_ALBUMID",
                "MUSICBRAINZ_ALBUMID",
                "MusicBrainz/Album Id"
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseArtistId,
                "MusicBrainz Album Artist Id",
                "MUSICBRAINZ_ALBUMARTISTID",
                "MUSICBRAINZ_ALBUMARTISTID",
                "MusicBrainz/Album Artist Id"
            ),
            new(
                SemanticAudioField.MusicBrainzTrackId,
                "MusicBrainz Track Id",
                "MUSICBRAINZ_TRACKID",
                "MUSICBRAINZ_TRACKID",
                "MusicBrainz/Track Id"
            ),
            new(
                SemanticAudioField.MusicBrainzDiscId,
                "MusicBrainz Disc Id",
                "MUSICBRAINZ_DISCID",
                "MUSICBRAINZ_DISCID",
                "MusicBrainz/Disc Id"
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseStatus,
                "MusicBrainz Album Status",
                "MUSICBRAINZ_ALBUMSTATUS",
                "MUSICBRAINZ_ALBUMSTATUS",
                "MusicBrainz/Album Status"
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseType,
                "MusicBrainz Album Type",
                "MUSICBRAINZ_ALBUMTYPE",
                "MUSICBRAINZ_ALBUMTYPE",
                "MusicBrainz/Album Type"
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseCountry,
                "MusicBrainz Album Release Country",
                "MUSICBRAINZ_RELEASECOUNTRY",
                "MUSICBRAINZ_RELEASECOUNTRY",
                "MusicBrainz/Album Release Country"
            ),
            new(SemanticAudioField.MusicIpId, "MusicIP PUID", "MUSICIP_PUID", "MUSICIP_PUID", "MusicIP/PUID"),
            new(SemanticAudioField.AmazonId, "ASIN", "ASIN", "ASIN", "ASIN"),
        ];
    }
}
