namespace Mfr.Filters
{
    /// <summary>
    /// Represents numeric count options used by extraction and trim filters.
    /// </summary>
    /// <param name="Count">
    /// Character count. Filters clamp to <c>[0, segment length]</c> when applying; the editor
    /// clamps edits to <c>0..9999</c>.
    /// </param>
    public sealed record CountFilterOptions(int Count);
}
