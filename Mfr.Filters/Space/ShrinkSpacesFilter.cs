using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Collapses runs of the current word-separator character to a single occurrence.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record ShrinkSpacesFilter(
        FilterTarget Target) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkSpaces";

        protected override string _TransformValue(string value, RenameItem item)
        {
            var ch = item.WordSeparator;
            var pattern = Regex.Escape(ch.ToString()) + "+";
            return Regex.Replace(value, pattern, _ => ch.ToString());
        }
    }
}
