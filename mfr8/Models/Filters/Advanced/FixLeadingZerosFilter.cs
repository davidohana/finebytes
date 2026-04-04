using System.Text.RegularExpressions;

namespace Mfr8.Models
{
    /// <summary>
    /// Options for normalizing numeric leading zeros.
    /// </summary>
    /// <param name="Width">Target numeric width.</param>
    /// <param name="RemoveExtraZeros">Whether extra leading zeros are removed before padding.</param>
    public sealed record FixLeadingZerosOptions(
        int Width,
        bool RemoveExtraZeros);

    /// <summary>
    /// Normalizes leading zeros in numeric sequences.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Leading-zero normalization options.</param>
    public sealed partial record FixLeadingZerosFilter(
        bool Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "FixLeadingZeros";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Width <= 0
                ? segment
                : _DigitsRegex().Replace(segment, m =>
                {
                    var digits = m.Value;
                    if (Options.RemoveExtraZeros)
                    {
                        digits = digits.TrimStart('0');
                    }

                    if (digits.Length == 0)
                    {
                        digits = "0";
                    }

                    return digits.PadLeft(Options.Width, '0');
                });
        }

        [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
        private static partial Regex _DigitsRegex();
    }
}
