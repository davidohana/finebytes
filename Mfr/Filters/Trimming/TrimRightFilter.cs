using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Trims a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimRight";

        internal override string TransformSegment(string segment, RenameItem item, FilterChainContext context)
        {
            // Ensure count is within [0, segment.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, segment.Length);
            return segment[..^count];
        }
    }
}
