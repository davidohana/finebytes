using Mfr.Models.Tags.Xiph;

namespace Mfr.Models.Tags.Ape
{
    /// <summary>
    /// APE text item keys modeled for read, write, and semantic projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unknown on-disk items survive field-patch by omission; only keys listed in <see cref="All"/> are
    /// loaded or written. Item lookup is case-insensitive at the TagLib boundary. Catalog identifier
    /// keys share Vorbis-style spellings with <see cref="XiphKnownKeys"/> and alias those constants.
    /// </para>
    /// </remarks>
    public static class ApeKnownKeys
    {
        /// <summary>Title item.</summary>
        public const string Title = "Title";

        /// <summary>Album item.</summary>
        public const string Album = "Album";

        /// <summary>Artist / performers item.</summary>
        public const string Artist = "Artist";

        /// <summary>Album artist item.</summary>
        public const string AlbumArtist = "Album Artist";

        /// <summary>Composer item.</summary>
        public const string Composer = "Composer";

        /// <summary>Genre item.</summary>
        public const string Genre = "Genre";

        /// <summary>Comment item.</summary>
        public const string Comment = "Comment";

        /// <summary>Lyrics item.</summary>
        public const string Lyrics = "Lyrics";

        /// <summary>Copyright item.</summary>
        public const string Copyright = "Copyright";

        /// <summary>Grouping item.</summary>
        public const string Grouping = "Grouping";

        /// <summary>Year item.</summary>
        public const string Year = "Year";

        /// <summary>Track number item.</summary>
        public const string Track = "Track";

        /// <summary>Track count item.</summary>
        public const string TrackCount = "TrackCount";

        /// <summary>Disc number item.</summary>
        public const string Disc = "Disc";

        /// <summary>Disc count item.</summary>
        public const string DiscCount = "DiscCount";

        /// <summary>Beats-per-minute item.</summary>
        public const string BeatsPerMinute = "BPM";

        /// <summary>Conductor item.</summary>
        public const string Conductor = "Conductor";

        /// <summary>MusicBrainz artist ID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzArtistId"/>).</summary>
        public const string MusicBrainzArtistId = XiphKnownKeys.MusicBrainzArtistId;

        /// <summary>MusicBrainz album/release ID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzReleaseId"/>).</summary>
        public const string MusicBrainzReleaseId = XiphKnownKeys.MusicBrainzReleaseId;

        /// <summary>MusicBrainz album artist ID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzReleaseArtistId"/>).</summary>
        public const string MusicBrainzReleaseArtistId = XiphKnownKeys.MusicBrainzReleaseArtistId;

        /// <summary>MusicBrainz track ID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzTrackId"/>).</summary>
        public const string MusicBrainzTrackId = XiphKnownKeys.MusicBrainzTrackId;

        /// <summary>MusicBrainz disc ID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzDiscId"/>).</summary>
        public const string MusicBrainzDiscId = XiphKnownKeys.MusicBrainzDiscId;

        /// <summary>MusicBrainz album status item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzReleaseStatus"/>).</summary>
        public const string MusicBrainzReleaseStatus = XiphKnownKeys.MusicBrainzReleaseStatus;

        /// <summary>MusicBrainz album type item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzReleaseType"/>).</summary>
        public const string MusicBrainzReleaseType = XiphKnownKeys.MusicBrainzReleaseType;

        /// <summary>MusicBrainz release country item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicBrainzReleaseCountry"/>).</summary>
        public const string MusicBrainzReleaseCountry = XiphKnownKeys.MusicBrainzReleaseCountry;

        /// <summary>MusicIP PUID item (Vorbis-style; aliases <see cref="XiphKnownKeys.MusicIpId"/>).</summary>
        public const string MusicIpId = XiphKnownKeys.MusicIpId;

        /// <summary>Amazon ASIN item (Vorbis-style; aliases <see cref="XiphKnownKeys.AmazonId"/>).</summary>
        public const string AmazonId = XiphKnownKeys.AmazonId;

        /// <summary>
        /// Known keys in stable display order for Metadata field I/O.
        /// </summary>
        public static IReadOnlyList<string> All { get; } =
        [
            Title,
            Album,
            Artist,
            AlbumArtist,
            Composer,
            Genre,
            Comment,
            Lyrics,
            Copyright,
            Grouping,
            Year,
            Track,
            TrackCount,
            Disc,
            DiscCount,
            BeatsPerMinute,
            Conductor,
            MusicBrainzArtistId,
            MusicBrainzReleaseId,
            MusicBrainzReleaseArtistId,
            MusicBrainzTrackId,
            MusicBrainzDiscId,
            MusicBrainzReleaseStatus,
            MusicBrainzReleaseType,
            MusicBrainzReleaseCountry,
            MusicIpId,
            AmazonId,
        ];
    }
}
