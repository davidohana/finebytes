using System.Text.Json.Serialization;

namespace Mfr.Models.Media
{
    /// <summary>
    /// Which filesystem timestamp field date/time filters modify.
    /// </summary>
    public enum TimestampField
    {
        /// <summary>
        /// Creation time.
        /// </summary>
        [JsonStringEnumMemberName("creation")]
        Creation,

        /// <summary>
        /// Last write time.
        /// </summary>
        [JsonStringEnumMemberName("lastWrite")]
        LastWrite,

        /// <summary>
        /// Last access time.
        /// </summary>
        [JsonStringEnumMemberName("lastAccess")]
        LastAccess,
    }
}
