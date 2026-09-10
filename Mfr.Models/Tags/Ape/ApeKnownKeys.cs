namespace Mfr.Models.Tags.Ape
{
    /// <summary>
    /// APE text item keys modeled for read, write, and semantic projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unknown on-disk items survive field-patch by omission; only keys listed in <see cref="All"/> are
    /// loaded or written. Item lookup is case-insensitive at the TagLib boundary.
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

        /// <summary>MusicBrainz artist ID item.</summary>
        public const string MusicBrainzArtistId = "MUSICBRAINZ_ARTISTID";

        /// <summary>MusicBrainz album/release ID item.</summary>
        public const string MusicBrainzReleaseId = "MUSICBRAINZ_ALBUMID";

        /// <summary>MusicBrainz album artist ID item.</summary>
        public const string MusicBrainzReleaseArtistId = "MUSICBRAINZ_ALBUMARTISTID";

        /// <summary>MusicBrainz track ID item.</summary>
        public const string MusicBrainzTrackId = "MUSICBRAINZ_TRACKID";

        /// <summary>MusicBrainz disc ID item.</summary>
        public const string MusicBrainzDiscId = "MUSICBRAINZ_DISCID";

        /// <summary>MusicBrainz album status item.</summary>
        public const string MusicBrainzReleaseStatus = "MUSICBRAINZ_ALBUMSTATUS";

        /// <summary>MusicBrainz album type item.</summary>
        public const string MusicBrainzReleaseType = "MUSICBRAINZ_ALBUMTYPE";

        /// <summary>MusicBrainz release country item.</summary>
        public const string MusicBrainzReleaseCountry = "MUSICBRAINZ_RELEASECOUNTRY";

        /// <summary>MusicIP PUID item.</summary>
        public const string MusicIpId = "MUSICIP_PUID";

        /// <summary>Amazon ASIN item.</summary>
        public const string AmazonId = "ASIN";

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
