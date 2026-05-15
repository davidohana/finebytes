using System.Collections.Immutable;
using Mfr.Models.Tags;
using TagLib;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Builds minimal <see cref="AudioTagOverlay"/> snapshots backed by serialized ID3v2 bytes for isolated tests.
    /// </summary>
    internal static class AudioTagOverlayTestBuilder
    {
        /// <summary>
        /// Creates an MPEG-style overlay carrying an ID3v2 tag with the supplied merged semantic fields serialized into frames.
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
            var id3 = new TagLib.Id3v2.Tag
            {
                Title = title ?? string.Empty,
                Album = album ?? string.Empty,
                Genres = string.IsNullOrWhiteSpace(genre) ? [] : [genre.Trim()],
                Comment = comment ?? string.Empty,
                Lyrics = lyrics ?? string.Empty,
                Copyright = copyright ?? string.Empty,
                Grouping = grouping ?? string.Empty,
                Year = year,
                Track = track,
                TrackCount = trackCount,
                Disc = disc,
                DiscCount = discCount,
                Performers = _SplitList(performersJoined),
                AlbumArtists = _SplitList(albumArtistsJoined),
                Composers = _SplitList(composersJoined)
            };

            return new AudioTagOverlay { Id3v2 = _SnapshotId3v2(id3) };
        }

        private static string[] _SplitList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split([";"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(static s => s.Trim())];
        }

        /// <remarks>
        /// Mirrors <c>Mfr.Metadata.AudioTagPersistence</c> snapshot shape so overlays round-trip parsing in projections.
        /// </remarks>
        private static Id3v2TagData _SnapshotId3v2(TagLib.Id3v2.Tag id3v2)
        {
            var fullRender = id3v2.Render();
            var canonicalTagBytes = ImmutableArray.Create(fullRender.Data);
            var canonicalTag = new TagLib.Id3v2.Tag(fullRender);
            var version = canonicalTag.Version;
            var list = new List<Id3v2SerializedFrame>();

            foreach (var frame in canonicalTag)
            {
                var rendered = frame.Render(version);
                var frameId = frame.FrameId.ToString(StringType.Latin1);
                list.Add(new Id3v2SerializedFrame
                {
                    FrameId = frameId,
                    Data = ImmutableArray.Create(rendered.Data),
                });
            }

            list.Sort(static (a, b) =>
            {
                var id = string.CompareOrdinal(a.FrameId, b.FrameId);
                if (id != 0)
                    return id;

                return a.Data.AsSpan().SequenceCompareTo(b.Data.AsSpan());
            });

            return new Id3v2TagData
            {
                Version = version,
                CanonicalTagBytes = canonicalTagBytes,
                Frames = [.. list],
            };
        }
    }
}
