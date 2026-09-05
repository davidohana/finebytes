using Mfr.Utils;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Options for illegal/custom character cleanup.
    /// </summary>
    /// <param name="RemoveIllegalChars">Whether illegal file-name characters are removed/replaced.</param>
    /// <param name="CustomCharsToRemove">Custom characters to remove/replace.</param>
    /// <param name="Replacement">Replacement value for both illegal and custom characters.</param>
    public sealed record CleanerOptions(bool RemoveIllegalChars, string CustomCharsToRemove, string Replacement);

    /// <summary>
    /// Cleans illegal and custom characters.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Cleaner options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Replace, "Cleaner")]
    public sealed record CleanerFilter(FilterTarget Target, CleanerOptions Options, StringApplyScope? ApplyScope = null)
        : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with add-to-list defaults (file prefix, illegal chars on, MFR7 custom cleanup list).
        /// </summary>
        public CleanerFilter()
            : this(
                new FilePrefixTarget(),
                new CleanerOptions(
                    RemoveIllegalChars: true,
                    CustomCharsToRemove: @"!""#$%&'()*+,/:;<=>?@[]\^`{}|~",
                    Replacement: ""
                )
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Cleaner";

        protected override string _TransformValue(string value, RenameItem item)
        {
            // Windows illegal-name set: this product renames Windows files (see WindowsFileNameChars).
            var invalidChars = Options.RemoveIllegalChars ? WindowsFileNameChars.Invalid : [];
            var customChars = Options.CustomCharsToRemove ?? "";
            var chars = invalidChars.Concat(customChars).ToHashSet();

            if (chars.Count == 0)
            {
                return value;
            }

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (chars.Contains(c))
                {
                    sb.Append(Options.Replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
