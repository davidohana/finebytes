using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for <see cref="CapitalizeAfterFilter"/>.
    /// </summary>
    /// <param name="CapitalizeAfterChars">
    /// Characters after which the following character is uppercased.
    /// </param>
    public sealed record CapitalizeAfterOptions(
        string CapitalizeAfterChars = ",!()[]{};-");

    /// <summary>
    /// Uppercases each letter which appears after one of the characters in the defined list.
    /// <para>
    /// Other characters are left unchanged.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Filter options.</param>
    public sealed record CapitalizeAfterFilter(
        FilterTarget Target,
        CapitalizeAfterOptions Options) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "CapitalizeAfter";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (string.IsNullOrEmpty(segment) || string.IsNullOrEmpty(Options.CapitalizeAfterChars))
            {
                return segment;
            }

            var capitalizeAfterSet = new HashSet<char>(Options.CapitalizeAfterChars);
            var chars = segment.ToCharArray();
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
