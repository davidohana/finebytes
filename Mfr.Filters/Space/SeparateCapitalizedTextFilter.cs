using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Inserts the current word-separator character between certain character pairs so that
    /// camel-case words, and boundaries between letters and digits, become separated tokens.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <remarks>
    /// A separator is inserted between two adjacent characters when:
    /// <list type="bullet">
    /// <item>The first is lowercase and the second is uppercase.</item>
    /// <item>The first is a letter and the second is a digit.</item>
    /// <item>The first is a digit and the second is a letter.</item>
    /// </list>
    /// The inserted character is the current word separator (U+0020 SPACE by default; set by a preceding
    /// <c>SpaceCharacter</c> filter).
    /// </remarks>
    public sealed record SeparateCapitalizedTextFilter(
        FilterTarget Target) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SeparateCapitalizedText";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (segment.Length <= 1)
            {
                return segment;
            }

            var separator = item.WordSeparator.ToString();
            var builder = new StringBuilder(segment.Length + 8);
            builder.Append(segment[0]);
            for (var i = 1; i < segment.Length; i++)
            {
                var previous = segment[i - 1];
                var current = segment[i];
                if (_ShouldInsertSeparator(previous, current))
                {
                    builder.Append(separator);
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static bool _ShouldInsertSeparator(char previous, char current)
        {
            var isLowerThenUpper = char.IsLower(previous) && char.IsUpper(current);
            var isLetterThenDigit = char.IsLetter(previous) && char.IsDigit(current);
            var isDigitThenLetter = char.IsDigit(previous) && char.IsLetter(current);
            return isLowerThenUpper || isLetterThenDigit || isDigitThenLetter;
        }
    }
}
