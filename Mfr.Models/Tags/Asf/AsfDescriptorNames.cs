namespace Mfr.Models.Tags.Asf
{
    /// <summary>
    /// Canonical ASF overlay row names matching TagLib’s ASF field mapping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Title"/>, <see cref="Author"/>, and <see cref="Copyright"/> are ASF Content Description
    /// Object fields (not extended content descriptors). Persistence must read/write them via TagLib’s
    /// <c>Title</c> / <c>Performers</c> / <c>Copyright</c> properties, never <c>AddDescriptor</c>.
    /// </para>
    /// <para>
    /// Remaining names are extended content descriptors. Comment is <see cref="Comment"/> (<c>WM/Text</c>);
    /// disc and disc-count share <see cref="PartOfSet"/> as <c>disc</c> or <c>disc/count</c>.
    /// </para>
    /// </remarks>
    public static class AsfDescriptorNames
    {
        /// <summary>Content Description Object title.</summary>
        public const string Title = "Title";

        /// <summary>Content Description Object author (performers, <c>; </c>-joined).</summary>
        public const string Author = "Author";

        /// <summary>Content Description Object copyright.</summary>
        public const string Copyright = "Copyright";

        /// <summary>Extended descriptor for user comment (<c>WM/Text</c>).</summary>
        public const string Comment = "WM/Text";

        /// <summary>Album title extended descriptor.</summary>
        public const string Album = "WM/AlbumTitle";

        /// <summary>Album artist extended descriptor.</summary>
        public const string AlbumArtist = "WM/AlbumArtist";

        /// <summary>Composer extended descriptor.</summary>
        public const string Composer = "WM/Composer";

        /// <summary>Genre extended descriptor.</summary>
        public const string Genre = "WM/Genre";

        /// <summary>Lyrics extended descriptor.</summary>
        public const string Lyrics = "WM/Lyrics";

        /// <summary>Content group / grouping extended descriptor.</summary>
        public const string Grouping = "WM/ContentGroupDescription";

        /// <summary>Year extended descriptor.</summary>
        public const string Year = "WM/Year";

        /// <summary>Track number extended descriptor.</summary>
        public const string TrackNumber = "WM/TrackNumber";

        /// <summary>Track total extended descriptor (TagLib name, not <c>WM/TrackTotal</c>).</summary>
        public const string TrackTotal = "TrackTotal";

        /// <summary>Disc / disc-count extended descriptor (<c>disc</c> or <c>disc/count</c>).</summary>
        public const string PartOfSet = "WM/PartOfSet";

        /// <summary>Beats-per-minute extended descriptor.</summary>
        public const string BeatsPerMinute = "WM/BeatsPerMinute";

        /// <summary>Conductor extended descriptor.</summary>
        public const string Conductor = "WM/Conductor";

        /// <summary>MusicBrainz artist ID extended descriptor.</summary>
        public const string MusicBrainzArtistId = "MusicBrainz/Artist Id";

        /// <summary>MusicBrainz album/release ID extended descriptor.</summary>
        public const string MusicBrainzReleaseId = "MusicBrainz/Album Id";

        /// <summary>MusicBrainz album artist ID extended descriptor.</summary>
        public const string MusicBrainzReleaseArtistId = "MusicBrainz/Album Artist Id";

        /// <summary>MusicBrainz track ID extended descriptor.</summary>
        public const string MusicBrainzTrackId = "MusicBrainz/Track Id";

        /// <summary>MusicBrainz disc ID extended descriptor.</summary>
        public const string MusicBrainzDiscId = "MusicBrainz/Disc Id";

        /// <summary>MusicBrainz album status extended descriptor.</summary>
        public const string MusicBrainzReleaseStatus = "MusicBrainz/Album Status";

        /// <summary>MusicBrainz album type extended descriptor.</summary>
        public const string MusicBrainzReleaseType = "MusicBrainz/Album Type";

        /// <summary>MusicBrainz album release country extended descriptor.</summary>
        public const string MusicBrainzReleaseCountry = "MusicBrainz/Album Release Country";

        /// <summary>MusicIP PUID extended descriptor.</summary>
        public const string MusicIpId = "MusicIP/PUID";

        /// <summary>Amazon ASIN extended descriptor.</summary>
        public const string AmazonId = "ASIN";
    }
}
