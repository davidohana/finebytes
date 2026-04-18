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
    public enum FileNamePart
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
    [JsonDerivedType(typeof(AttributesTarget), "Attributes")]
    public abstract record FilterTarget
    {
        /// <summary>
        /// Gets the target family for serialization and dispatch.
        /// </summary>
        [JsonIgnore]
        public abstract FilterTargetFamily Family { get; }
    }

    /// <summary>
    /// Targets file-name content using a selected part.
    /// </summary>
    /// <param name="FileNamePart">The file-name part to target.</param>
    public sealed record FileNameTarget(FileNamePart FileNamePart) : FilterTarget
    {
        /// <summary>
        /// Gets the target family.
        /// </summary>
        public override FilterTargetFamily Family => FilterTargetFamily.FileName;
    }

    /// <summary>
    /// Targets filesystem attributes (read-only, hidden, archive, system).
    /// </summary>
    public sealed record AttributesTarget : FilterTarget
    {
        /// <inheritdoc />
        public override FilterTargetFamily Family => FilterTargetFamily.Attributes;
    }
}
