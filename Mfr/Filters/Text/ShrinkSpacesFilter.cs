using Mfr.Models;

namespace Mfr.Filters.Text
{
    /// <summary>
    /// Collapses runs of whitespace into single spaces.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record ShrinkSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkSpaces";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            return TextFilterRegexCache.WhitespaceRegex.Replace(segment, " ");
        }
    }
}
