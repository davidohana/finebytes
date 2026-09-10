namespace Mfr.Models.Tags.Xiph
{
    /// <summary>
    /// Xiph/Vorbis comment keys modeled for read, write, and Filter Options Apply-To.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unknown on-disk keys survive field-patch by omission; only keys listed here are loaded or written.
    /// </para>
    /// </remarks>
    public static class XiphKnownKeys
    {
        /// <summary>MusicBrainz artist ID comment key.</summary>
        public const string MusicBrainzArtistId = "MUSICBRAINZ_ARTISTID";

        /// <summary>MusicBrainz album/release ID comment key.</summary>
        public const string MusicBrainzReleaseId = "MUSICBRAINZ_ALBUMID";

        /// <summary>MusicBrainz album artist ID comment key.</summary>
        public const string MusicBrainzReleaseArtistId = "MUSICBRAINZ_ALBUMARTISTID";

        /// <summary>MusicBrainz track ID comment key.</summary>
        public const string MusicBrainzTrackId = "MUSICBRAINZ_TRACKID";

        /// <summary>MusicBrainz disc ID comment key.</summary>
        public const string MusicBrainzDiscId = "MUSICBRAINZ_DISCID";

        /// <summary>MusicBrainz album status comment key.</summary>
        public const string MusicBrainzReleaseStatus = "MUSICBRAINZ_ALBUMSTATUS";

        /// <summary>MusicBrainz album type comment key.</summary>
        public const string MusicBrainzReleaseType = "MUSICBRAINZ_ALBUMTYPE";

        /// <summary>MusicBrainz release country comment key.</summary>
        public const string MusicBrainzReleaseCountry = "MUSICBRAINZ_RELEASECOUNTRY";

        /// <summary>MusicIP PUID comment key.</summary>
        public const string MusicIpId = "MUSICIP_PUID";

        /// <summary>Amazon ASIN comment key.</summary>
        public const string AmazonId = "ASIN";

        /// <summary>
        /// Known keys in stable display order for Filter Options and Metadata field I/O.
        /// </summary>
        public static IReadOnlyList<string> All { get; } =
        [
            "TITLE",
            "ALBUM",
            "ARTIST",
            "ALBUMARTIST",
            "COMPOSER",
            "GENRE",
            "DESCRIPTION",
            "COMMENT",
            "LYRICS",
            "UNSYNCEDLYRICS",
            "COPYRIGHT",
            "GROUPING",
            "CONTENTGROUP",
            "DATE",
            "YEAR",
            "TRACKNUMBER",
            "TRACKTOTAL",
            "TOTALTRACKS",
            "DISCNUMBER",
            "DISCTOTAL",
            "TOTALDISCS",
            "BPM",
            "TEMPO",
            "CONDUCTOR",
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
