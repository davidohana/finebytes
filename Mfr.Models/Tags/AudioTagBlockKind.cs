using System.Text.Json.Serialization;

namespace Mfr.Models.Tags
{
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
}
