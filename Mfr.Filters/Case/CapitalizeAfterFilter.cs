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
        CapitalizeAfterOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "CapitalizeAfter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(Options.CapitalizeAfterChars))
            {
                return value;
            }

            var capitalizeAfterSet = new HashSet<char>(Options.CapitalizeAfterChars);
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
