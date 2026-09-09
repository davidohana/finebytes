namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for <see cref="CapitalizeAfterFilter"/>.
    /// </summary>
    /// <param name="CapitalizeAfterChars">
    /// Characters after which the following character is uppercased.
    /// </param>
    public sealed record CapitalizeAfterOptions(string CapitalizeAfterChars = ",!()[]{};-");

    /// <summary>
    /// Uppercases each letter which appears after one of the characters in the defined list.
    /// <para>
    /// Other characters are left unchanged.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Filter options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Case, "Capitalize After")]
    public sealed record CapitalizeAfterFilter(
        FilterTarget Target,
        CapitalizeAfterOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        private HashSet<char>? _capitalizeAfterChars;

        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, default trigger characters).
        /// </summary>
        public CapitalizeAfterFilter()
            : this(new FilePrefixTarget(), new CapitalizeAfterOptions()) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "CapitalizeAfter";

        /// <inheritdoc />
        protected override void _Setup()
        {
            // Unconditional assign (BaseFilter._Setup): `with` copies this field; empty list must clear it.
            _capitalizeAfterChars = string.IsNullOrEmpty(Options.CapitalizeAfterChars)
                ? null
                : [.. Options.CapitalizeAfterChars];
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            var capitalizeAfterSet = _capitalizeAfterChars;
            if (string.IsNullOrEmpty(value) || capitalizeAfterSet is null)
            {
                return value;
            }

            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length - 1; i++)
            {
                if (capitalizeAfterSet.Contains(chars[i]))
                {
                    chars[i + 1] = char.ToUpperInvariant(chars[i + 1]);
                }
            }

            return new string(chars);
        }
    }
}
