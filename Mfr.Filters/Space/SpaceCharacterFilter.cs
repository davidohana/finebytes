namespace Mfr.Filters.Space
{
    /// <summary>
    /// Options for defining the word-separator character and mapping common separators to it.
    /// </summary>
    /// <param name="SpaceCharacter">Single character used as the word separator for later filters.</param>
    /// <param name="Replacements">Substrings replaced with the space character, applied in list order.</param>
    public sealed record SpaceCharacterOptions(char SpaceCharacter, IReadOnlyList<string> Replacements)
    {
        /// <summary>MFR7 add-to-list defaults: %20, space, underscore.</summary>
        public static IReadOnlyList<string> DefaultReplacements { get; } = ["%20", " ", "_"];
    }

    /// <summary>
    /// Defines the word-separator character and optionally maps common separators to that character.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Space definition and replacement options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Space, "Space Character")]
    public sealed record SpaceCharacterFilter(
        FilterTarget Target,
        SpaceCharacterOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, space separator, common replacements).
        /// </summary>
        public SpaceCharacterFilter()
            : this(new FilePrefixTarget(), new SpaceCharacterOptions(' ', SpaceCharacterOptions.DefaultReplacements)) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceCharacter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            item.WordSeparator = Options.SpaceCharacter;
            var sep = Options.SpaceCharacter.ToString();
            var result = value;
            foreach (var from in Options.Replacements)
            {
                result = result.Replace(from, sep, StringComparison.Ordinal);
            }

            return result;
        }
    }
}
