using System.Text.Json.Serialization;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// Represents a polymorphic filter target (file-name slices, paths, ancestor segments, audio overlay fields); JSON uses property <c>targetType</c> as the discriminator.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "targetType")]
    [JsonDerivedType(typeof(FilePrefixTarget), "FilePrefix")]
    [JsonDerivedType(typeof(FileExtensionTarget), "FileExtension")]
    [JsonDerivedType(typeof(FileFullNameTarget), "FileFullName")]
    [JsonDerivedType(typeof(AncestorFolderTarget), "AncestorFolder")]
    [JsonDerivedType(typeof(FullPathTarget), "FullPath")]
    [JsonDerivedType(typeof(ParentDirectoryTarget), "ParentDirectory")]
    [JsonDerivedType(typeof(SemanticAudioFieldTarget), "SemanticAudioField")]
    [JsonDerivedType(typeof(Id3v1FieldTarget), "Id3v1Field")]
    [JsonDerivedType(typeof(Id3v2FrameTarget), "Id3v2Frame")]
    [JsonDerivedType(typeof(XiphFieldTarget), "XiphField")]
    public abstract record FilterTarget;

    /// <summary>
    /// Marker for <see cref="FilterTarget"/> types that address <see cref="Rename.FileMeta.AudioTagOverlay"/> fields or frames.
    /// </summary>
    public interface IAudioOverlayFilterTarget;

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

    /// <summary>
    /// Targets one cross-format field on the semantic projection of <see cref="Rename.FileMeta.AudioTagOverlay"/>; string filters read/write text or decimal-digit numeric strings.
    /// </summary>
    /// <param name="Field">Which <see cref="SemanticAudioField"/> is addressed.</param>
    public sealed record SemanticAudioFieldTarget(SemanticAudioField Field) : FilterTarget, IAudioOverlayFilterTarget;

    /// <summary>
    /// Targets one ID3v1 scalar on <see cref="Rename.FileMeta.AudioTagOverlay"/>.<see cref="AudioTagOverlay.Id3v1"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unsupported on non-MPEG containers (PreviewError). Creates an empty ID3v1 block when absent on a capable file.
    /// </para>
    /// </remarks>
    /// <param name="Field">Which ID3v1 scalar is addressed.</param>
    public sealed record Id3v1FieldTarget(Id3v1Field Field) : FilterTarget, IAudioOverlayFilterTarget;

    /// <summary>
    /// Targets one modeled ID3v2 frame on <see cref="Rename.FileMeta.AudioTagOverlay"/>.<see cref="AudioTagOverlay.Id3v2"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Singleton frames (for example <c>TIT2</c>) use <paramref name="FrameId"/> only.
    /// Multi-instance frames (<c>COMM</c>, <c>USLT</c>, <c>TXXX</c>) also match <paramref name="Language"/> /
    /// <paramref name="Description"/>; omit both for the primary <c>COMM</c>/<c>USLT</c> instance
    /// (empty description). Unsupported on non-MPEG containers. Writing a v2.4-only frame (for example
    /// <c>TDRC</c>) into a v2.3 tag is a PreviewError — the version is never upgraded silently.
    /// </para>
    /// </remarks>
    /// <param name="FrameId">Four-character frame id (case-insensitive; stored uppercase).</param>
    /// <param name="Language">ISO-639-2 language for <c>COMM</c>/<c>USLT</c>, or <see langword="null"/> when not applicable.</param>
    /// <param name="Description">Content descriptor for <c>COMM</c>/<c>USLT</c>/<c>TXXX</c>, or <see langword="null"/> for primary.</param>
    public sealed record Id3v2FrameTarget(
        string FrameId,
        string? Language = null,
        string? Description = null) : FilterTarget, IAudioOverlayFilterTarget;

    /// <summary>
    /// Targets one known Xiph / Vorbis comment key on <see cref="Rename.FileMeta.AudioTagOverlay"/>.<see cref="AudioTagOverlay.Xiph"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keys are matched case-insensitively and stored uppercase. Unsupported outside FLAC/Ogg-style containers.
    /// </para>
    /// </remarks>
    /// <param name="Key">Comment field key (for example <c>TITLE</c>, <c>ARTIST</c>).</param>
    public sealed record XiphFieldTarget(string Key) : FilterTarget, IAudioOverlayFilterTarget;
}
