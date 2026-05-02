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
    /// Represents a polymorphic filter target (file name parts and folder-ancestor segments).
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
    [JsonDerivedType(typeof(FileNameTarget), "FileName")]
    [JsonDerivedType(typeof(AncestorFolderTarget), "AncestorFolder")]
    public abstract record FilterTarget;

    /// <summary>
    /// Targets file-name content using a selected part.
    /// </summary>
    /// <param name="FileNamePart">The file-name part to target.</param>
    public sealed record FileNameTarget(FileNamePart FileNamePart) : FilterTarget;

    /// <summary>
    /// Targets one ancestor folder segment name relative to the item’s preview directory path.
    /// </summary>
    /// <para>
    /// <paramref name="Level"/> counts from the folder that contains the file:
    /// <c>1</c> is that folder’s name, <c>2</c> is its parent’s name, and so on toward the volume root.
    /// The value must be at least <c>1</c> and cannot exceed how many folders appear under <see cref="M:System.IO.Path.GetPathRoot(System.String)"/>.
    /// </para>
    /// <param name="Level">Ancestor distance counting from immediate parent upward.</param>
    public sealed record AncestorFolderTarget(int Level) : FilterTarget;
}
