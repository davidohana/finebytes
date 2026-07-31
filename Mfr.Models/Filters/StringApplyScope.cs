using System.Text.Json.Serialization;

namespace Mfr.Models
{
    /// <summary>
    /// Endpoint anchor for <see cref="SubstringApplyScope"/> positions (1-based inclusive from that side).
    /// </summary>
    public enum StringScopeAnchor
    {
        /// <summary>
        /// Positions count from the first character toward the end.
        /// </summary>
        [JsonStringEnumMemberName("left")]
        Left,

        /// <summary>
        /// Positions count from the last character toward the start (1 = last character).
        /// </summary>
        [JsonStringEnumMemberName("right")]
        Right
    }

    /// <summary>
    /// Optional restriction for <c>StringTargetFilter</c> — run the filter on a substring or token only,
    /// then splice the result back into the full preview field.
    /// </summary>
    /// <remarks>
    /// <para>Omit the property on a filter or use <see langword="null"/> to transform the entire target string (default).</para>
    /// <para>JSON uses property <c>scopeType</c> as the discriminator (same pattern as <see cref="FilterTarget"/>).</para>
    /// </remarks>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "scopeType")]
    [JsonDerivedType(typeof(SubstringApplyScope), "Substring")]
    [JsonDerivedType(typeof(TokenApplyScope), "Token")]
    public abstract record StringApplyScope;

    /// <summary>
    /// Apply the string filter only to an inclusive substring of the target, defined by 1-based positions from either end.
    /// </summary>
    /// <param name="StartPosition">1-based index of the substring start (inclusive after resolution).</param>
    /// <param name="StartAnchor">Whether <paramref name="StartPosition"/> counts from the left or the right.</param>
    /// <param name="EndPosition">1-based index of the substring end (inclusive after resolution).</param>
    /// <param name="EndAnchor">Whether <paramref name="EndPosition"/> counts from the left or the right.</param>
    public sealed record SubstringApplyScope(
        [property: JsonPropertyName("startPosition")] int StartPosition,
        [property: JsonPropertyName("startAnchor")] StringScopeAnchor StartAnchor,
        [property: JsonPropertyName("endPosition")] int EndPosition,
        [property: JsonPropertyName("endAnchor")] StringScopeAnchor EndAnchor) : StringApplyScope;

    /// <summary>
    /// Split the target by <paramref name="Separator"/> and apply the filter only to the selected 1-based token.
    /// </summary>
    /// <param name="Separator">Delimiter string (same rules as formatter <c>&lt;token:&gt;</c>).</param>
    /// <param name="TokenNumber">1-based token index (must be at least 1).</param>
    public sealed record TokenApplyScope(
        [property: JsonPropertyName("separator")] string Separator,
        [property: JsonPropertyName("tokenNumber")] int TokenNumber) : StringApplyScope;
}
