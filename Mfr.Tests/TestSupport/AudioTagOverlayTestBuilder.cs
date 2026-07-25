using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Builds minimal <see cref="AudioTagOverlay"/> snapshots with modeled ID3v2 fields for isolated tests.
    /// </summary>
    internal static class AudioTagOverlayTestBuilder
    {
        /// <summary>
        /// Creates an MPEG-style overlay carrying an ID3v2 tag with the supplied merged semantic fields.
        /// </summary>
        public static AudioTagOverlay Id3Overlay(
            string? title = null,
            string? album = null,
            string? performersJoined = null,
            string? albumArtistsJoined = null,
            string? composersJoined = null,
            string? genre = null,
            string? comment = null,
            string? lyrics = null,
            string? copyright = null,
            string? grouping = null,
            uint year = 0,
            uint track = 0,
            uint trackCount = 0,
            uint disc = 0,
            uint discCount = 0)
        {
            var overlay = new AudioTagOverlay
            {
                ContainerFormat = AudioContainerFormat.Mpeg,
                Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
            };

            var merged = new SemanticAudioTag(
                Title: string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                Album: string.IsNullOrWhiteSpace(album) ? null : album.Trim(),
                Performers: string.IsNullOrWhiteSpace(performersJoined) ? null : performersJoined.Trim(),
                AlbumArtists: string.IsNullOrWhiteSpace(albumArtistsJoined) ? null : albumArtistsJoined.Trim(),
                Composers: string.IsNullOrWhiteSpace(composersJoined) ? null : composersJoined.Trim(),
                Genre: string.IsNullOrWhiteSpace(genre) ? null : genre.Trim(),
                Comment: string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                Lyrics: string.IsNullOrWhiteSpace(lyrics) ? null : lyrics.Trim(),
                Copyright: string.IsNullOrWhiteSpace(copyright) ? null : copyright.Trim(),
                Grouping: string.IsNullOrWhiteSpace(grouping) ? null : grouping.Trim(),
                Year: year == 0 ? null : year,
                Track: track == 0 ? null : track,
                TrackCount: trackCount == 0 ? null : trackCount,
                Disc: disc == 0 ? null : disc,
                DiscCount: discCount == 0 ? null : discCount);

            // Keep an empty Id3v2 block present so filter merges have a target (do not prune before first write).
            if (!merged.ContainsRenderableSemantics())
                return overlay;

            AudioTagPersistence.MergeSemanticIntoBlocks(overlay, merged);
            return overlay;
        }
    }
}
