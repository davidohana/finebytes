using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for letter-case transformations.
    /// </summary>
    /// <param name="Mode">Case transformation mode.</param>
    /// <param name="SkipWords">Words to leave lowercased in title case mode.</param>
    /// <param name="WeirdUppercaseChancePercent">
    /// Uppercase chance in percent for <see cref="LettersCaseMode.WeirdCase"/> (clamped to 0..100).
    /// </param>
    /// <param name="WeirdFixedPlaces">
    /// For <see cref="LettersCaseMode.WeirdCase"/>: when true, casing decisions depend only on character
    /// position so the same index gets the same case across different names; when false, per-name variation
    /// is included.
    /// </param>
    /// <param name="SentenceEndChars">
    /// Characters that mark sentence endings for <see cref="LettersCaseMode.SentenceCase"/>.
    /// When empty, sentence case only capitalizes at the start of the string.
    /// </param>
    public sealed record LettersCaseOptions(
        LettersCaseMode Mode,
        IReadOnlyList<string> SkipWords,
        int WeirdUppercaseChancePercent = 50,
        bool WeirdFixedPlaces = false,
        string SentenceEndChars = ".!?");

    /// <summary>
    /// Supported letter-case transformation modes.
    /// </summary>
    /// <remarks>
    /// <para><b>Title case</b> and <b>sentence case</b> differ as follows: title case capitalizes
    /// each segment between occurrences of the current word separator (skip-words apply per segment).
    /// Sentence case lowercases the whole string, then capitalizes the first letter of the text and
    /// the first letter after <c>.</c> <c>!</c> or <c>?</c> when followed by one or more occurrences of
    /// the current word separator (U+0020 SPACE by default).</para>
    /// <para>See each enum member for a concrete before/after example.</para>
    /// </remarks>
    public enum LettersCaseMode
    {
        /// <summary>Every letter is converted to uppercase.</summary>
        /// <example>
        /// <para><c>Hello World</c> → <c>HELLO WORLD</c></para>
        /// </example>
        UpperCase,

        /// <summary>Every letter is converted to lowercase.</summary>
        /// <example>
        /// <para><c>Hello World</c> → <c>hello world</c></para>
        /// </example>
        LowerCase,

        /// <summary>
        /// Uppercases the first character (index 0) and lowercases the remainder of the segment.
        /// </summary>
        /// <example>
        /// <para><c>hELLO world</c> → <c>Hello world</c></para>
        /// <para><c> 123_aBC</c> → <c> 123_abc</c> (leading character is unchanged because it is not a letter).</para>
        /// </example>
        FirstLetterUp,

        /// <summary>
        /// Applies mixed/random casing to letters using a configurable uppercase chance.
        /// </summary>
        /// <example>
        /// <para><c>hello world</c> → <c>hElLo wOrLd</c> (example outcome)</para>
        /// </example>
        WeirdCase,

        /// <summary>
        /// Capitalizes the first letter of each segment between occurrences of the current word separator
        /// (U+0020 SPACE by default; set by <c>SpaceCharacter</c> when used earlier in the chain);
        /// words in <see cref="LettersCaseOptions.SkipWords"/> stay lowercase.
        /// </summary>
        /// <example>
        /// <para>Typical: <c>hello world</c> → <c>Hello World</c>.</para>
        /// <para>With skip-words <c>a</c>, <c>the</c>, <c>for</c>: <c>a song for the world</c> → <c>a Song for the World</c>.</para>
        /// </example>
        TitleCase,

        /// <summary>
        /// Lowercases the string, then capitalizes the first letter of the whole string and the first
        /// letter after configured sentence-end characters (see <see cref="LettersCaseOptions.SentenceEndChars"/>)
        /// when followed by one or more word-separator characters (same as title case: default U+0020 SPACE;
        /// set by <c>SpaceCharacter</c> when used earlier in the chain).
        /// </summary>
        /// <example>
        /// <para>Default separator (space): <c>hello world. next line.</c> → <c>Hello world. Next line.</c></para>
        /// <para>Separator <c>_</c>: <c>hello._world._again</c> → <c>Hello._World._Again</c>.</para>
        /// </example>
        SentenceCase,

        /// <summary>Uppercase letters become lowercase and lowercase letters become uppercase.</summary>
        /// <example>
        /// <para><c>Hello</c> → <c>hELLO</c></para>
        /// </example>
        InvertCase
    }

    /// <summary>
    /// Converts text letter casing.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Case transformation options.</param>
    public sealed record LettersCaseFilter(
        bool Enabled,
        FilterTarget Target,
        LettersCaseOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "LettersCase";

        internal override string TransformSegment(string segment, RenameItem item, FilterChainContext context)
        {
            return Options.Mode switch
            {
                LettersCaseMode.UpperCase => segment.ToUpperInvariant(),
                LettersCaseMode.LowerCase => segment.ToLowerInvariant(),
                LettersCaseMode.FirstLetterUp => _FirstLetterUp(segment),
                LettersCaseMode.WeirdCase => _ApplyWeirdCase(
                    input: segment,
                    item: item,
                    weirdUppercaseChancePercent: Options.WeirdUppercaseChancePercent,
                    weirdFixedPlaces: Options.WeirdFixedPlaces),
                LettersCaseMode.TitleCase => _ApplyTitleCase(segment, Options.SkipWords, item.WordSeparator),
                LettersCaseMode.SentenceCase => _ApplySentenceCase(segment, item.WordSeparator, Options.SentenceEndChars),
                LettersCaseMode.InvertCase => _InvertCase(segment),
                _ => segment
            };
        }

        private static string _FirstLetterUp(string input)
        {
            if (input.Length == 0)
            {
                return input;
            }

            if (input.Length == 1)
            {
                return char.ToUpperInvariant(input[0]).ToString();
            }

            return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
        }

        private static string _ApplyWeirdCase(
            string input,
            RenameItem item,
            int weirdUppercaseChancePercent,
            bool weirdFixedPlaces)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var uppercaseChancePercent = Math.Clamp(weirdUppercaseChancePercent, 0, 100);
            if (uppercaseChancePercent == 0)
            {
                return input.ToLowerInvariant();
            }

            if (uppercaseChancePercent == 100)
            {
                return input.ToUpperInvariant();
            }

            var chars = input.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!char.IsLetter(c))
                {
                    continue;
                }

                var itemSeed = weirdFixedPlaces ? 0 : item.Original.GlobalIndex;
                var score = _GetPseudoRandomPercent(position: i, itemSeed: itemSeed);
                chars[i] = score < uppercaseChancePercent
                    ? char.ToUpperInvariant(c)
                    : char.ToLowerInvariant(c);
            }

            return new string(chars);
        }

        private static int _GetPseudoRandomPercent(int position, int itemSeed)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = (hash ^ (uint)position) * 16777619u;
                hash = (hash ^ (uint)itemSeed) * 16777619u;
                return (int)(hash % 100u);
            }
        }

        private static string _ApplyTitleCase(string input, IReadOnlyList<string> skipWords, char wordSeparator)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var skipWordToIsExcluded = new HashSet<string>(skipWords, StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder(input.Length);
            var i = 0;
            while (i < input.Length)
            {
                while (i < input.Length && input[i] == wordSeparator)
                {
                    sb.Append(wordSeparator);
                    i++;
                }

                if (i >= input.Length)
                {
                    break;
                }

                var start = i;
                while (i < input.Length && input[i] != wordSeparator)
                {
                    i++;
                }

                var word = input[start..i];
                sb.Append(_TitleCaseOneWord(word, skipWordToIsExcluded));
            }

            return sb.ToString();
        }

        private static string _TitleCaseOneWord(string word, HashSet<string> skipWordToIsExcluded)
        {
            if (skipWordToIsExcluded.Contains(word))
            {
                return word.ToLowerInvariant();
            }

            if (word.Length == 0)
            {
                return word;
            }

            return word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        }

        private static string _ApplySentenceCase(string input, char wordSeparator, string sentenceEndChars)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var chars = input.ToLowerInvariant().ToCharArray();
            if (_IsAsciiLowerLetter(chars[0]))
            {
                chars[0] = char.ToUpperInvariant(chars[0]);
            }

            if (sentenceEndChars.Length == 0)
            {
                return new string(chars);
            }

            var sentenceEndToIsIncluded = new HashSet<char>(sentenceEndChars.Where(c => c != wordSeparator));
            if (sentenceEndToIsIncluded.Count == 0)
            {
                return new string(chars);
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (!sentenceEndToIsIncluded.Contains(chars[i]))
                {
                    continue;
                }

                var j = i + 1;
                if (j >= chars.Length || chars[j] != wordSeparator)
                {
                    continue;
                }

                while (j < chars.Length && chars[j] == wordSeparator)
                {
                    j++;
                }

                if (j >= chars.Length || !_IsAsciiLowerLetter(chars[j]))
                {
                    continue;
                }

                chars[j] = char.ToUpperInvariant(chars[j]);
            }

            return new string(chars);
        }

        private static bool _IsAsciiLowerLetter(char c)
        {
            return c is >= 'a' and <= 'z';
        }

        private static string _InvertCase(string input)
        {
            var chars = input.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (char.IsUpper(c))
                {
                    chars[i] = char.ToLowerInvariant(c);
                }
                else if (char.IsLower(c))
                {
                    chars[i] = char.ToUpperInvariant(c);
                }
            }

            return new string(chars);
        }

    }
}
