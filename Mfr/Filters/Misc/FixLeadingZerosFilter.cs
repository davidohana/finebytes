using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Misc
{
    /// <summary>
    /// Options for normalizing numeric leading zeros.
    /// </summary>
    /// <param name="Width">Target numeric width.</param>
    /// <param name="RemoveExtraZeros">Whether extra leading zeros are removed before padding.</param>
    /// <param name="MaxCount">Maximum count of numbers to fix (0 for all).</param>
    /// <param name="WholeWordOnly">Whether to fix only numbers that form a whole word (not part of a word).</param>
    public sealed record FixLeadingZerosOptions(
        int Width,
        bool RemoveExtraZeros,
        int MaxCount = 0,
        bool WholeWordOnly = false);

    /// <summary>
    /// Normalizes leading zeros in numeric sequences.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Leading-zero normalization options.</param>
    public sealed partial record FixLeadingZerosFilter(
        bool Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "FixLeadingZeros";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (Options.Width <= 0)
            {
                return segment;
            }

            var count = 0;
            return _DigitsRegex().Replace(segment, m =>
            {
                if (Options.WholeWordOnly)
                {
                    var start = m.Index;
                    var end = m.Index + m.Length;

                    var isLetterBefore = start > 0 && char.IsLetter(segment[start - 1]);
                    var isLetterAfter = end < segment.Length && char.IsLetter(segment[end]);

                    if (isLetterBefore || isLetterAfter)
                    {
                        return m.Value;
                    }
                }

                if (Options.MaxCount > 0 && count >= Options.MaxCount)
                {
                    return m.Value;
                }

                count++;

                var digits = m.Value;
                if (Options.RemoveExtraZeros)
                {
                    digits = digits.TrimStart('0');
                }

                if (digits.Length == 0)
                {
                    digits = "0";
                }

                if (digits.Length >= Options.Width)
                {
                    return digits;
                }

                return digits.PadLeft(Options.Width, '0');
            });
        }

        [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
        private static partial Regex _DigitsRegex();
    }
}
