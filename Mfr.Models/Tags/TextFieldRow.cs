using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// One known text key and its values in a Xiph or APE tag snapshot.
    /// </summary>
    /// <param name="Key">Canonical field key (typically uppercase for Xiph).</param>
    /// <param name="Values">Trimmed non-empty values for that key.</param>
    public readonly record struct TextFieldRow(string Key, ImmutableArray<string> Values);
}
