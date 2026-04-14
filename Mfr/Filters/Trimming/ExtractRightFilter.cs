using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Extracts a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            // Ensure count is within [0, segment.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, segment.Length);
            return segment[^count..];
        }
    }
}
