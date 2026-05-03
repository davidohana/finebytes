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
    /// Represents a polymorphic filter target (file-name slices, paths, ancestor segments); JSON uses property <c>targetType</c> as the discriminator.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "targetType")]
    [JsonDerivedType(typeof(FilePrefixTarget), "FilePrefix")]
    [JsonDerivedType(typeof(FileExtensionTarget), "FileExtension")]
    [JsonDerivedType(typeof(FileFullNameTarget), "FileFullName")]
    [JsonDerivedType(typeof(AncestorFolderTarget), "AncestorFolder")]
    [JsonDerivedType(typeof(FullPathTarget), "FullPath")]
    [JsonDerivedType(typeof(ParentDirectoryTarget), "ParentDirectory")]
    public abstract record FilterTarget;

    /// <summary>
    /// Targets the file name without extension (<c>prefix</c> segment).
    /// </summary>
    public sealed record FilePrefixTarget : FilterTarget;

    /// <summary>
    /// Targets the file extension including the leading dot (<c>extension</c> segment).
    /// </summary>
    public sealed record FileExtensionTarget : FilterTarget;

    /// <summary>
    /// Targets the full file name (<c>prefix + extension</c>); writes parse with <see cref="M:System.IO.Path.GetFileName(System.String)"/> rules.
    /// </summary>
    public sealed record FileFullNameTarget : FilterTarget;

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

    /// <summary>
    /// Targets the item’s preview full file path (<c>directory + prefix + extension</c>).
    /// </summary>
    /// <remarks>
    /// Writes must assign a fully qualified path that includes directory and file name.
    /// </remarks>
    public sealed record FullPathTarget : FilterTarget;

    /// <summary>
    /// Targets the preview containing-directory absolute path (<c>DirectoryPath</c> on preview metadata).
    /// </summary>
    /// <remarks>
    /// Writes assign the containing-folder path only; the preview prefix and extension stay unchanged.
    /// </remarks>
    public sealed record ParentDirectoryTarget : FilterTarget;
}
