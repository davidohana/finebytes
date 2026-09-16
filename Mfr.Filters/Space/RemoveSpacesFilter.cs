namespace Mfr.Filters.Space
{
    /// <summary>
    /// Removes all occurrences of the current word-separator character.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Space, "Remove Spaces")]
    public sealed record RemoveSpacesFilter(FilterTarget Target, StringApplyScope? ApplyScope = null)
        : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file name target).
        /// </summary>
        public RemoveSpacesFilter()
            : this(new FileNameTarget()) { }

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
