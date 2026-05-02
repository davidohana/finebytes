using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Options for defining the word-separator character and mapping common separators to it.
    /// </summary>
    /// <param name="SpaceCharacter">Single character used as the word separator for later filters.</param>
    /// <param name="ReplaceSpaces">When true, replaces U+0020 SPACE with the defined space character.</param>
    /// <param name="ReplaceUnderscores">When true, replaces underscore with the defined space character.</param>
    /// <param name="ReplacePercent20">When true, replaces the literal percent-twenty sequence with the defined space character.</param>
    /// <param name="CustomText">When non-empty, replaces this substring with the defined space character; when empty, has no effect.</param>
    public sealed record SpaceCharacterOptions(
        char SpaceCharacter,
        bool ReplaceSpaces,
        bool ReplaceUnderscores,
        bool ReplacePercent20,
        string CustomText);

    /// <summary>
    /// Defines the word-separator character and optionally maps common separators to that character.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Space definition and replacement options.</param>
    public sealed record SpaceCharacterFilter(
        FilterTarget Target,
        SpaceCharacterOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceCharacter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            item.WordSeparator = Options.SpaceCharacter;
            var sep = Options.SpaceCharacter.ToString();
            var result = value;
            foreach (var from in _GetReplacementSourceStrings(Options))
            {
                result = result.Replace(from, sep, StringComparison.Ordinal);
            }

            return result;
        }

        private static List<string> _GetReplacementSourceStrings(SpaceCharacterOptions options)
        {
            var sources = new List<string>(capacity: 4);
            if (options.ReplacePercent20)
            {
                sources.Add("%20");
            }

            if (options.ReplaceSpaces)
            {
                sources.Add(" ");
            }

            if (options.ReplaceUnderscores)
            {
                sources.Add("_");
            }

            if (options.CustomText.Length > 0)
            {
                sources.Add(options.CustomText);
            }

            return sources;
        }
    }
}
