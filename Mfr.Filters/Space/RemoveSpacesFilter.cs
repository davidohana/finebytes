using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Removes all occurrences of the current word-separator character.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record RemoveSpacesFilter(
        FilterTarget Target) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "RemoveSpaces";

        protected override string _TransformValue(string value, RenameItem item)
        {
            return value.Replace(item.WordSeparator.ToString(), "", StringComparison.Ordinal);
        }
    }
}
