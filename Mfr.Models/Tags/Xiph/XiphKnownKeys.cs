namespace Mfr.Models.Tags.Xiph
{
    /// <summary>
    /// Xiph/Vorbis comment keys modeled for read, write, and Filter Options Apply-To.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unknown on-disk keys survive field-patch by omission; only keys listed in <see cref="All"/> are
    /// loaded or written. Apply-To labels/tips live in <see cref="XiphKeyLabels"/>.
    /// </para>
    /// </remarks>
    public static class XiphKnownKeys
    {
        /// <summary>Title comment key.</summary>
        public const string Title = "TITLE";

        /// <summary>Album comment key.</summary>
        public const string Album = "ALBUM";

        /// <summary>Artist / performers comment key.</summary>
        public const string Artist = "ARTIST";

        /// <summary>Album artist comment key.</summary>
        public const string AlbumArtist = "ALBUMARTIST";

        /// <summary>Composer comment key.</summary>
        public const string Composer = "COMPOSER";

        /// <summary>Genre comment key.</summary>
        public const string Genre = "GENRE";

        /// <summary>Description comment key (preferred comment field for merge).</summary>
        public const string Description = "DESCRIPTION";

        /// <summary>Comment comment key (alias; cleared on semantic merge).</summary>
        public const string Comment = "COMMENT";

        /// <summary>Lyrics comment key.</summary>
        public const string Lyrics = "LYRICS";

        /// <summary>Unsynced lyrics comment key (alias; cleared on semantic merge).</summary>
        public const string UnsyncedLyrics = "UNSYNCEDLYRICS";

        /// <summary>Copyright comment key.</summary>
        public const string Copyright = "COPYRIGHT";

        /// <summary>Grouping comment key.</summary>
        public const string Grouping = "GROUPING";

        /// <summary>Content group comment key (alias; cleared on semantic merge).</summary>
        public const string ContentGroup = "CONTENTGROUP";

        /// <summary>Date comment key (preferred year field for merge).</summary>
        public const string Date = "DATE";

        /// <summary>Year comment key (alias; cleared on semantic merge).</summary>
        public const string Year = "YEAR";

        /// <summary>Track number comment key.</summary>
        public const string TrackNumber = "TRACKNUMBER";

        /// <summary>Track total comment key.</summary>
        public const string TrackTotal = "TRACKTOTAL";

        /// <summary>Total tracks comment key (alias; cleared on semantic merge).</summary>
        public const string TotalTracks = "TOTALTRACKS";

        /// <summary>Disc number comment key.</summary>
        public const string DiscNumber = "DISCNUMBER";

        /// <summary>Disc total comment key.</summary>
        public const string DiscTotal = "DISCTOTAL";

        /// <summary>Total discs comment key (alias; cleared on semantic merge).</summary>
        public const string TotalDiscs = "TOTALDISCS";

        /// <summary>Beats-per-minute comment key.</summary>
        public const string BeatsPerMinute = "BPM";

        /// <summary>Tempo comment key (alias; cleared on semantic merge).</summary>
        public const string Tempo = "TEMPO";

        /// <summary>Conductor comment key.</summary>
        public const string Conductor = "CONDUCTOR";

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
            Title,
            Album,
            Artist,
            AlbumArtist,
            Composer,
            Genre,
            Description,
            Comment,
            Lyrics,
            UnsyncedLyrics,
            Copyright,
            Grouping,
            ContentGroup,
            Date,
            Year,
            TrackNumber,
            TrackTotal,
            TotalTracks,
            DiscNumber,
            DiscTotal,
            TotalDiscs,
            BeatsPerMinute,
            Tempo,
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
