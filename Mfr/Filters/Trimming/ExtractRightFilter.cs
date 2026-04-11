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
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            return Options.Count <= 0 ? "" : segment.Length <= Options.Count ? segment : segment[^Options.Count..];
        }
    }
}
