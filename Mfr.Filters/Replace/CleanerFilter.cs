using System.Text;
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
        private HashSet<char>? _charsToClean;

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

        /// <inheritdoc />
        protected override void _Setup()
        {
            // Unconditional assign (BaseFilter._Setup): `with` copies this field; clearing options must rebuild.
            var customChars = Options.CustomCharsToRemove ?? "";
            var chars = customChars.ToHashSet();
            if (Options.RemoveIllegalChars)
            {
                // Windows illegal-name set: this product renames Windows files (see WindowsFileNameChars).
                WindowsFileNameChars.AddInvalidTo(chars);
            }

            _charsToClean = chars.Count == 0 ? null : chars;
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            var chars = _charsToClean;
            if (chars is null)
            {
                return value;
            }

            var replacement = Options.Replacement ?? "";
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (!chars.Contains(c))
                {
                    sb.Append(c);
                    continue;
                }

                sb.Append(replacement);
            }

            return sb.ToString();
        }
    }
}
