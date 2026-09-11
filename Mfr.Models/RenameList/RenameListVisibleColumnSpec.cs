using System.Text.Json.Serialization;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One Rename List visible grid column for persistence: field identity plus optional width.
    /// <para>
    /// Shared by <c>session.json</c> <c>renameList.visibleColumns</c> and preset <c>visibleColumns</c>
    /// (<c>key</c> / <c>width</c>).
    /// </para>
    /// </summary>
    /// <param name="Key">Field key (original or preview).</param>
    /// <param name="Width">
    /// Column width in pixels, or <see langword="null"/> to use catalog/header defaults on restore.
    /// </param>
    public sealed record RenameListVisibleColumnSpec(
        [property: JsonPropertyName("key")] RenameListFieldKey Key,
        [property: JsonPropertyName("width")] int? Width = null
    );
}
