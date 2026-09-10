using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Picard/TagLib key names for cross-format catalog identifier fields on <see cref="SemanticAudioTag"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ID3v2 stores these as <c>TXXX</c> content descriptors; Xiph/APE use Vorbis-style keys; ASF uses
    /// extended content descriptor names. Apple freeform atoms and RIFF INFO are not mapped here.
    /// Xiph, APE, and ASF columns reference <see cref="XiphKnownKeys"/>, <see cref="ApeKnownKeys"/>, and
    /// <see cref="AsfDescriptorNames"/> so catalog IDs cannot drift from those owners.
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
                XiphKnownKeys.MusicBrainzArtistId,
                ApeKnownKeys.MusicBrainzArtistId,
                AsfDescriptorNames.MusicBrainzArtistId
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseId,
                "MusicBrainz Album Id",
                XiphKnownKeys.MusicBrainzReleaseId,
                ApeKnownKeys.MusicBrainzReleaseId,
                AsfDescriptorNames.MusicBrainzReleaseId
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseArtistId,
                "MusicBrainz Album Artist Id",
                XiphKnownKeys.MusicBrainzReleaseArtistId,
                ApeKnownKeys.MusicBrainzReleaseArtistId,
                AsfDescriptorNames.MusicBrainzReleaseArtistId
            ),
            new(
                SemanticAudioField.MusicBrainzTrackId,
                "MusicBrainz Track Id",
                XiphKnownKeys.MusicBrainzTrackId,
                ApeKnownKeys.MusicBrainzTrackId,
                AsfDescriptorNames.MusicBrainzTrackId
            ),
            new(
                SemanticAudioField.MusicBrainzDiscId,
                "MusicBrainz Disc Id",
                XiphKnownKeys.MusicBrainzDiscId,
                ApeKnownKeys.MusicBrainzDiscId,
                AsfDescriptorNames.MusicBrainzDiscId
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseStatus,
                "MusicBrainz Album Status",
                XiphKnownKeys.MusicBrainzReleaseStatus,
                ApeKnownKeys.MusicBrainzReleaseStatus,
                AsfDescriptorNames.MusicBrainzReleaseStatus
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseType,
                "MusicBrainz Album Type",
                XiphKnownKeys.MusicBrainzReleaseType,
                ApeKnownKeys.MusicBrainzReleaseType,
                AsfDescriptorNames.MusicBrainzReleaseType
            ),
            new(
                SemanticAudioField.MusicBrainzReleaseCountry,
                "MusicBrainz Album Release Country",
                XiphKnownKeys.MusicBrainzReleaseCountry,
                ApeKnownKeys.MusicBrainzReleaseCountry,
                AsfDescriptorNames.MusicBrainzReleaseCountry
            ),
            new(
                SemanticAudioField.MusicIpId,
                "MusicIP PUID",
                XiphKnownKeys.MusicIpId,
                ApeKnownKeys.MusicIpId,
                AsfDescriptorNames.MusicIpId
            ),
            new(
                SemanticAudioField.AmazonId,
                "ASIN",
                XiphKnownKeys.AmazonId,
                ApeKnownKeys.AmazonId,
                AsfDescriptorNames.AmazonId
            ),
        ];
    }
}
