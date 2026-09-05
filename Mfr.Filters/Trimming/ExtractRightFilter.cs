namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Extracts a fixed number of characters from the right.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Trimming, "Extract Right")]
    public sealed record ExtractRightFilter(
        FilterTarget Target,
        CountFilterOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope), ICountOptionsFilter
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, extract one character).
        /// </summary>
        public ExtractRightFilter()
            : this(new FilePrefixTarget(), new CountFilterOptions(Count: 1)) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";

        /// <inheritdoc />
        public BaseFilter WithOptions(CountFilterOptions options)
        {
            return this with { Options = options };
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            return value[^Options.ClampToLength(value.Length)..];
        }
    }
}
