using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Extracts a fixed number of characters from the right.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractRightFilter(
        FilterTarget Target,
        CountFilterOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";

        protected override string _TransformValue(string value, RenameItem item)
        {
            // Ensure count is within [0, value.Length] to avoid IndexOutOfRangeException
            var count = Math.Clamp(Options.Count, 0, value.Length);
            return value[^count..];
        }
    }
}
