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
            "MUSICBRAINZ_ARTISTID",
            "MUSICBRAINZ_ALBUMID",
            "MUSICBRAINZ_ALBUMARTISTID",
            "MUSICBRAINZ_TRACKID",
            "MUSICBRAINZ_DISCID",
            "MUSICBRAINZ_ALBUMSTATUS",
            "MUSICBRAINZ_ALBUMTYPE",
            "MUSICBRAINZ_RELEASECOUNTRY",
            "MUSICIP_PUID",
            "ASIN",
        ];
    }
}
