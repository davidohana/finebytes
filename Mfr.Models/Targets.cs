using System.Text.Json.Serialization;

namespace Mfr.Models
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
        LastAccess
    }

    /// <summary>
    /// Selects which file-name segment a file-name target addresses.
    /// </summary>
    public enum FileNamePart
    {
        Prefix,
        Extension,
        Full
    }

    /// <summary>
    /// Represents a polymorphic filter target for file-name segment filters.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
    [JsonDerivedType(typeof(FileNameTarget), "FileName")]
    public abstract record FilterTarget;

    /// <summary>
    /// Targets file-name content using a selected part.
    /// </summary>
    /// <param name="FileNamePart">The file-name part to target.</param>
    public sealed record FileNameTarget(FileNamePart FileNamePart) : FilterTarget;
}
