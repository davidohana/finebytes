using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Trims a fixed number of characters from the left.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimLeftFilter(
        FilterTarget Target,
        CountFilterOptions Options) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimLeft";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            // Ensure count is within [0, segment.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, segment.Length);
            return segment[count..];
        }
    }
}
