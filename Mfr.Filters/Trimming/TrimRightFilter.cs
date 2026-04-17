using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Trims a fixed number of characters from the right.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimRightFilter(
        FilterTarget Target,
        CountFilterOptions Options) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimRight";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            // Ensure count is within [0, segment.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, segment.Length);
            return segment[..^count];
        }
    }
}
