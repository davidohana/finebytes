namespace Mfr.Filters
{
    /// <summary>
    /// Represents numeric count options used by extraction and trim filters.
    /// </summary>
    /// <param name="Count">
    /// Character count. Filters clamp to <c>[0, segment length]</c> when applying; the editor
    /// clamps edits to <c>0..9999</c>.
    /// </param>
    public sealed record CountFilterOptions(int Count)
    {
        /// <summary>
        /// Clamps <see cref="Count"/> to <c>[0, length]</c>.
        /// </summary>
        /// <param name="length">Segment length to clamp against (typically the transform value length).</param>
        /// <returns>Count in <c>[0, length]</c>.</returns>
        public int ClampToLength(int length)
        {
            return Math.Clamp(Count, 0, length);
        }
    }
}
