using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Removes all occurrences of the current word-separator character.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record RemoveSpacesFilter(
        FilterTarget Target) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "RemoveSpaces";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            return segment.Replace(item.WordSeparator.ToString(), "", StringComparison.Ordinal);
        }
    }
}
