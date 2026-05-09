using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Collapses runs of the current word-separator character to a single occurrence.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    public sealed record ShrinkSpacesFilter(
        FilterTarget Target, StringApplyScope? ApplyScope = null) : StringTargetFilter(Target, ApplyScope)
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
