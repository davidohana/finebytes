using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Removes all occurrences of the current word-separator character.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record RemoveSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "RemoveSpaces";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            return segment.Replace(item.WordSeparator.ToString(), "", StringComparison.Ordinal);
        }
    }
}
