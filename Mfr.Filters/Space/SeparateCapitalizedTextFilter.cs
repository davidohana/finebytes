using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Inserts word separators between camelCase and letter/digit boundaries.
    /// <para>
    /// Uses the current word-separator character (default U+0020 SPACE; see remarks) between pairs so
    /// camel-case words and letter/digit boundaries become separate tokens.
    /// </para>
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
        FilterTarget Target) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SeparateCapitalizedText";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (value.Length <= 1)
            {
                return value;
            }

            var separator = item.WordSeparator.ToString();
            var builder = new StringBuilder(value.Length + 8);
            builder.Append(value[0]);
            for (var i = 1; i < value.Length; i++)
            {
                var previous = value[i - 1];
                var current = value[i];
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
