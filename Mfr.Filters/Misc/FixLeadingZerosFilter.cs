using System.Text.RegularExpressions;

namespace Mfr.Filters.Misc
{
    /// <summary>
    /// Options for normalizing numeric leading zeros.
    /// </summary>
    /// <param name="Width">Target numeric width.</param>
    /// <param name="RemoveExtraZeros">Whether extra leading zeros are removed before padding.</param>
    /// <param name="MaxCount">Maximum count of digit groups to change (0 for all).</param>
    /// <param name="WholeWordOnly">Whether to fix only numbers that form a whole word (not part of a word).</param>
    public sealed record FixLeadingZerosOptions(
        int Width,
        bool RemoveExtraZeros,
        int MaxCount = 0,
        bool WholeWordOnly = true
    );

    /// <summary>
    /// Normalizes leading zeros in numeric sequences.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Leading-zero normalization options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Misc, "Fix Leading 0's")]
    public sealed partial record FixLeadingZerosFilter(
        FilterTarget Target,
        FixLeadingZerosOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, width 2, first number only, whole-word).
        /// </summary>
        public FixLeadingZerosFilter()
            : this(
                new FilePrefixTarget(),
                new FixLeadingZerosOptions(Width: 2, RemoveExtraZeros: false, MaxCount: 1, WholeWordOnly: true)
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "FixLeadingZeros";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (Options.Width <= 0)
            {
                return value;
            }

            var changedCount = 0;
            return _DigitsRegex()
                .Replace(
                    value,
                    m =>
                    {
                        if (Options.WholeWordOnly && !_IsWholeWordDigitGroup(value, m.Index, m.Length))
                        {
                            return m.Value;
                        }

                        var normalized = _NormalizeDigitGroup(m.Value, Options);
                        if (normalized == m.Value)
                        {
                            return m.Value;
                        }

                        if (Options.MaxCount > 0 && changedCount >= Options.MaxCount)
                        {
                            return m.Value;
                        }

                        changedCount++;
                        return normalized;
                    }
                );
        }

        /// <summary>
        /// Returns whether the digit span is not adjacent to a letter (MFR7 whole-word for numeric runs).
        /// </summary>
        private static bool _IsWholeWordDigitGroup(string value, int start, int length)
        {
            var end = start + length;
            var isLetterBefore = start > 0 && char.IsLetter(value[start - 1]);
            var isLetterAfter = end < value.Length && char.IsLetter(value[end]);
            return !isLetterBefore && !isLetterAfter;
        }

        /// <summary>
        /// Pads or optionally strips leading zeros so the digit group meets <see cref="FixLeadingZerosOptions.Width"/>.
        /// </summary>
        private static string _NormalizeDigitGroup(string digits, FixLeadingZerosOptions options)
        {
            if (options.RemoveExtraZeros)
            {
                digits = digits.TrimStart('0');
            }

            if (digits.Length == 0)
            {
                digits = "0";
            }

            if (digits.Length >= options.Width)
            {
                return digits;
            }

            return digits.PadLeft(options.Width, '0');
        }

        [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
        private static partial Regex _DigitsRegex();
    }
}
