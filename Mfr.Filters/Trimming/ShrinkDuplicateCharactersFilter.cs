using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Options for <see cref="ShrinkDuplicateCharactersFilter"/>.
    /// </summary>
    /// <param name="Character">Character whose adjacent duplicate occurrences are collapsed.</param>
    public sealed record ShrinkDuplicateCharactersOptions(
        char Character);

    /// <summary>
    /// Collapses adjacent duplicate occurrences of a configured character to a single occurrence.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Filter options.</param>
    public sealed record ShrinkDuplicateCharactersFilter(
        FilterTarget Target,
        ShrinkDuplicateCharactersOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkDuplicateCharacters";

        protected override string _TransformValue(string value, RenameItem item)
        {
            var pattern = Regex.Escape(Options.Character.ToString()) + "+";
            return Regex.Replace(value, pattern, _ => Options.Character.ToString());
        }
    }
}
