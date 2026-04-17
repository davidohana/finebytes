using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Collapses runs of the current word-separator character to a single occurrence.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record ShrinkSpacesFilter(
        bool Enabled,
        FilterTarget Target) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkSpaces";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            var ch = item.WordSeparator;
            var pattern = Regex.Escape(ch.ToString()) + "+";
            return Regex.Replace(segment, pattern, _ => ch.ToString());
        }
    }
}
