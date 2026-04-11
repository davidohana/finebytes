using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Uppercases dot-separated initials patterns inside text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An initials pattern is one or more single letters separated by dots, such as
    /// <c>u.s.a</c>, <c>d.j</c>, or <c>e.x.a.m.p.l.e</c>.
    /// </para>
    /// <para>
    /// Only letters inside detected initials patterns are uppercased; all other characters stay unchanged.
    /// </para>
    /// </remarks>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record UppercaseInitialsFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "UppercaseInitials";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return segment;
            }

            var chars = segment.ToCharArray();
            var i = 0;
            while (i < chars.Length)
            {
                if (!_TryGetInitialsRange(chars, i, out var rangeEndExclusive))
                {
                    i++;
                    continue;
                }

                for (var j = i; j < rangeEndExclusive; j++)
                {
                    if (char.IsLetter(chars[j]))
                    {
                        chars[j] = char.ToUpperInvariant(chars[j]);
                    }
                }

                i = rangeEndExclusive;
            }

            return new string(chars);
        }

        private static bool _TryGetInitialsRange(char[] chars, int start, out int rangeEndExclusive)
        {
            rangeEndExclusive = start;
            if (!char.IsLetter(chars[start]))
            {
                return false;
            }

            var index = start;
            var segmentCount = 0;

            while (index < chars.Length && char.IsLetter(chars[index]))
            {
                var letterStart = index;
                while (index < chars.Length && char.IsLetter(chars[index]))
                {
                    index++;
                }

                var letterCount = index - letterStart;
                if (letterCount != 1)
                {
                    break;
                }

                segmentCount++;
                if (index >= chars.Length || chars[index] != '.')
                {
                    break;
                }

                if (index + 1 >= chars.Length || !char.IsLetter(chars[index + 1]))
                {
                    break;
                }

                index++;
            }

            if (segmentCount < 2)
            {
                return false;
            }

            rangeEndExclusive = index;
            return true;
        }
    }
}
