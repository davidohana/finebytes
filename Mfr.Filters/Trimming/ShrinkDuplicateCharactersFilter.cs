namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Options for <see cref="ShrinkDuplicateCharactersFilter"/>.
    /// </summary>
    /// <param name="Character">
    /// Character whose adjacent duplicate occurrences are collapsed.
    /// <c>\0</c> (empty editor) is a no-op.
    /// </param>
    public sealed record ShrinkDuplicateCharactersOptions(char Character);

    /// <summary>
    /// Collapses adjacent duplicate occurrences of a configured character to a single occurrence.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Filter options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Trimming, "Shrink Duplicate Characters")]
    public sealed record ShrinkDuplicateCharactersFilter(
        FilterTarget Target,
        ShrinkDuplicateCharactersOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, hyphen duplicates).
        /// </summary>
        public ShrinkDuplicateCharactersFilter()
            : this(new FilePrefixTarget(), new ShrinkDuplicateCharactersOptions(Character: '-')) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkDuplicateCharacters";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (Options.Character == '\0')
            {
                return value;
            }

            return CharacterRunHelpers.CollapseAdjacentDuplicates(value, Options.Character);
        }
    }
}
