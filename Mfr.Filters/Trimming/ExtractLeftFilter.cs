using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Extracts a fixed number of characters from the left.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractLeftFilter(
        FilterTarget Target,
        CountFilterOptions Options) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractLeft";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            // Ensure count is within [0, segment.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, segment.Length);
            return segment[..count];
        }
    }
}
