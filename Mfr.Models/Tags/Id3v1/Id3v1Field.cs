using System.Text.Json.Serialization;

namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// Which scalar on an <see cref="Id3v1TagData"/> block an <see cref="Filters.Id3v1FieldTarget"/> addresses.
    /// </summary>
    public enum Id3v1Field
    {
        /// <summary>Track title.</summary>
        [JsonStringEnumMemberName("title")]
        Title,

        /// <summary>Artist.</summary>
        [JsonStringEnumMemberName("artist")]
        Artist,

        /// <summary>Album.</summary>
        [JsonStringEnumMemberName("album")]
        Album,

        /// <summary>Four-digit year.</summary>
        [JsonStringEnumMemberName("year")]
        Year,

        /// <summary>Comment.</summary>
        [JsonStringEnumMemberName("comment")]
        Comment,

        /// <summary>Track number (ID3v1.1).</summary>
        [JsonStringEnumMemberName("track")]
        Track,

        /// <summary>WinAmp genre name (or empty when unset / index 0).</summary>
        [JsonStringEnumMemberName("genre")]
        Genre,
    }
}
