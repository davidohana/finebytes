using System.Text.Json.Serialization;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Which embedded audio-tag overlay field a <see cref="AudioOverlayFieldTarget"/> addresses on <see cref="FileMeta.AudioTagOverlay"/>.
    /// </summary>
    public enum AudioOverlayField
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
    }
}
