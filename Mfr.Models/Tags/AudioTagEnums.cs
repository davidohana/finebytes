using System.Text.Json.Serialization;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// The physical audio container a rename row is backed by, which decides the tag blocks it can hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Detected once per preview cycle when embedded tags load. Containers we do not model resolve to
    /// <see cref="Unknown"/> and support no tag blocks.
    /// </para>
    /// </remarks>
    public enum AudioContainerFormat
    {
        /// <summary>Container is missing, unreadable, or not one this application models.</summary>
        Unknown,

        /// <summary>MPEG audio (MP3).</summary>
        Mpeg,

        /// <summary>Native FLAC stream.</summary>
        Flac,

        /// <summary>Ogg container (Vorbis, Opus, Ogg FLAC).</summary>
        Ogg,

        /// <summary>ISO base-media container (MP4, M4A, M4B).</summary>
        Mpeg4,

        /// <summary>Advanced Systems Format (WMA, WMV).</summary>
        Asf,

        /// <summary>RIFF container (WAV).</summary>
        Riff,

        /// <summary>Monkey's Audio stream (APE).</summary>
        Ape,
    }

    /// <summary>
    /// Identifies one embedded tag-block type modeled on <see cref="AudioTagOverlay"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each member maps one-to-one onto an <see cref="AudioTagOverlay"/> block property and onto the matching
    /// TagLib <c>TagTypes</c> value used when reading, creating, or removing that block.
    /// </para>
    /// </remarks>
    public enum AudioTagBlockKind
    {
        /// <summary>ID3v1 trailer (MPEG only).</summary>
        [JsonStringEnumMemberName("id3v1")]
        Id3v1,

        /// <summary>ID3v2 frame tag.</summary>
        [JsonStringEnumMemberName("id3v2")]
        Id3v2,

        /// <summary>Xiph/Vorbis comment (FLAC, Ogg, Opus).</summary>
        [JsonStringEnumMemberName("xiph")]
        Xiph,

        /// <summary>APEv2 tag.</summary>
        [JsonStringEnumMemberName("ape")]
        Ape,

        /// <summary>Apple/iTunes <c>ilst</c> metadata (MP4, M4A).</summary>
        [JsonStringEnumMemberName("apple")]
        Apple,

        /// <summary>ASF extended content descriptors (WMA).</summary>
        [JsonStringEnumMemberName("asf")]
        Asf,

        /// <summary>RIFF <c>LIST/INFO</c> chunk (WAV).</summary>
        [JsonStringEnumMemberName("riffInfo")]
        RiffInfo,
    }

    /// <summary>
    /// Which cross-format semantic audio field a <see cref="Filters.SemanticAudioFieldTarget"/> addresses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Members map one-to-one onto the common projected fields (title, album, track, and so on). Values are read from
    /// and written back into structured blocks on <see cref="AudioTagOverlay"/>; they are not properties of the overlay itself.
    /// </para>
    /// </remarks>
    public enum SemanticAudioField
    {
        /// <summary>Track title.</summary>
        [JsonStringEnumMemberName("title")]
        Title,

        /// <summary>Album name.</summary>
        [JsonStringEnumMemberName("album")]
        Album,

        /// <summary>Primary performers (joined display string).</summary>
        [JsonStringEnumMemberName("performers")]
        Performers,

        /// <summary>Album artists (joined display string).</summary>
        [JsonStringEnumMemberName("albumArtists")]
        AlbumArtists,

        /// <summary>Composers (joined display string).</summary>
        [JsonStringEnumMemberName("composers")]
        Composers,

        /// <summary>Genre.</summary>
        [JsonStringEnumMemberName("genre")]
        Genre,

        /// <summary>Comment.</summary>
        [JsonStringEnumMemberName("comment")]
        Comment,

        /// <summary>Lyrics.</summary>
        [JsonStringEnumMemberName("lyrics")]
        Lyrics,

        /// <summary>Copyright.</summary>
        [JsonStringEnumMemberName("copyright")]
        Copyright,

        /// <summary>Grouping.</summary>
        [JsonStringEnumMemberName("grouping")]
        Grouping,

        /// <summary>Release year when expressed as a tag number.</summary>
        [JsonStringEnumMemberName("year")]
        Year,

        /// <summary>Track index (number).</summary>
        [JsonStringEnumMemberName("track")]
        Track,

        /// <summary>Track count (of n/m).</summary>
        [JsonStringEnumMemberName("trackCount")]
        TrackCount,

        /// <summary>Disc index.</summary>
        [JsonStringEnumMemberName("disc")]
        Disc,

        /// <summary>Disc count.</summary>
        [JsonStringEnumMemberName("discCount")]
        DiscCount,

        /// <summary>Beats per minute.</summary>
        [JsonStringEnumMemberName("beatsPerMinute")]
        BeatsPerMinute,

        /// <summary>Conductor or director.</summary>
        [JsonStringEnumMemberName("conductor")]
        Conductor,

        /// <summary>MusicBrainz artist ID.</summary>
        [JsonStringEnumMemberName("musicBrainzArtistId")]
        MusicBrainzArtistId,

        /// <summary>MusicBrainz release (album) ID.</summary>
        [JsonStringEnumMemberName("musicBrainzReleaseId")]
        MusicBrainzReleaseId,

        /// <summary>MusicBrainz release (album) artist ID.</summary>
        [JsonStringEnumMemberName("musicBrainzReleaseArtistId")]
        MusicBrainzReleaseArtistId,

        /// <summary>MusicBrainz track ID.</summary>
        [JsonStringEnumMemberName("musicBrainzTrackId")]
        MusicBrainzTrackId,

        /// <summary>MusicBrainz disc ID.</summary>
        [JsonStringEnumMemberName("musicBrainzDiscId")]
        MusicBrainzDiscId,

        /// <summary>MusicBrainz release status.</summary>
        [JsonStringEnumMemberName("musicBrainzReleaseStatus")]
        MusicBrainzReleaseStatus,

        /// <summary>MusicBrainz release type.</summary>
        [JsonStringEnumMemberName("musicBrainzReleaseType")]
        MusicBrainzReleaseType,

        /// <summary>MusicBrainz release country.</summary>
        [JsonStringEnumMemberName("musicBrainzReleaseCountry")]
        MusicBrainzReleaseCountry,

        /// <summary>MusicIP PUID.</summary>
        [JsonStringEnumMemberName("musicIpId")]
        MusicIpId,

        /// <summary>Amazon ASIN.</summary>
        [JsonStringEnumMemberName("amazonId")]
        AmazonId,
    }
}
