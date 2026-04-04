using System.Text.Json.Serialization;

namespace Mfr.Models
{
    /// <summary>
    /// Identifies the kind of target data a filter can operate on.
    /// </summary>
    public enum FilterTargetFamily
    {
        FileName,
        FileExtension,
        FullFileName,
        FullFilePath,
        DirectorySegment,
        FileContents,
        Attributes,
        CreationDate,
        LastWriteDate,
        LastAccessDate,
        AudioTag,
        Id3v1,
        Id3v2,
        ImageTag
    }

    /// <summary>
    /// Selects which file-name segment a file-name target addresses.
    /// </summary>
    public enum FileNameTargetMode
    {
        Prefix,
        Extension,
        Full
    }

    /// <summary>
    /// Represents a polymorphic filter target.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
    [JsonDerivedType(typeof(FileNameTarget), "FileName")]
    public abstract record FilterTarget
    {
        /// <summary>
        /// Gets the target family for serialization and dispatch.
        /// </summary>
        [JsonIgnore]
        public abstract FilterTargetFamily Family { get; }
    }

    /// <summary>
    /// Targets file-name content using a selected mode.
    /// </summary>
    /// <param name="FileNameMode">The file-name segment to target.</param>
    public sealed record FileNameTarget(FileNameTargetMode FileNameMode) : FilterTarget
    {
        /// <summary>
        /// Gets the target family.
        /// </summary>
        public override FilterTargetFamily Family => FilterTargetFamily.FileName;
    }
}
