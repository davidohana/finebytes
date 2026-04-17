using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for casing-list based word casing.
    /// </summary>
    /// <param name="FilePath">Path to the casing-list text file (one word per line).</param>
    /// <param name="UppercaseSentenceInitial">
    /// When <c>true</c>, uppercases the first letter at string start and after sentence-end boundaries.
    /// </param>
    /// <param name="SentenceEndChars">
    /// Characters that mark sentence endings when <see cref="UppercaseSentenceInitial"/> is enabled.
    /// </param>
    public sealed record CasingListOptions(
        string FilePath,
        bool UppercaseSentenceInitial = false,
        string SentenceEndChars = ".!?");

    /// <summary>
    /// Changes each word's casing to match how it appears in a casing-list file.
    /// Words not found in the list are left unchanged.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Casing-list options.</param>
    public sealed record CasingListFilter(
        bool Enabled,
        FilterTarget Target,
        CasingListOptions Options) : BaseFilter(Enabled, Target)
    {
        private Dictionary<string, string>? _lowerWordToCasing;

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "CasingList";

        /// <summary>
        /// Loads and caches casing-list entries for this filter instance.
        /// </summary>
        protected override void _Setup()
        {
            _lowerWordToCasing = CasingListParser.ParseFile(Options.FilePath);
        }

        /// <summary>
        /// Applies casing-list normalization and optional sentence-initial uppercasing.
        /// </summary>
        /// <param name="segment">Input text segment to transform.</param>
        /// <param name="item">Current rename item carrying separator context.</param>
        /// <returns>The transformed text segment.</returns>
        protected override string _TransformSegment(string segment, RenameItem item)
        {
            var lowerWordToCasing = _lowerWordToCasing
                ?? throw new InvalidOperationException("Casing-list setup must complete before transform.");

            if (segment.Length == 0 || lowerWordToCasing.Count == 0)
            {
                return segment;
            }

            // First pass: normalize only words found in the casing list.
            var transformed = _ApplyCasingList(segment, item.WordSeparator, lowerWordToCasing);
            if (!Options.UppercaseSentenceInitial)
            {
                return transformed;
            }

            // Optional second pass: force sentence starts to uppercase.
            return _UppercaseSentenceInitials(
                input: transformed,
                wordSeparator: item.WordSeparator,
                sentenceEndChars: Options.SentenceEndChars);
        }

        /// <summary>
        /// Rewrites each separator-delimited token using the casing-list dictionary.
        /// </summary>
        /// <param name="input">Input text to process.</param>
        /// <param name="wordSeparator">Configured word separator character.</param>
        /// <param name="lowerWordToCasing">Lowercased-word to canonical-casing mapping.</param>
        /// <returns>Text with matched words replaced by canonical casing.</returns>
        private static string _ApplyCasingList(string input, char wordSeparator, IReadOnlyDictionary<string, string> lowerWordToCasing)
        {
            var output = new StringBuilder(input.Length);
            var token = new StringBuilder();

            // Stream through once so separator runs are preserved exactly.
            foreach (var c in input)
            {
                if (c == wordSeparator)
                {
                    _AppendResolvedToken(output, token, lowerWordToCasing);
                    output.Append(c);
                    continue;
                }

                token.Append(c);
            }

            _AppendResolvedToken(output, token, lowerWordToCasing);
            return output.ToString();
        }

        /// <summary>
        /// Appends the buffered token to output, using list casing when a match exists.
        /// </summary>
        /// <param name="output">Destination text builder.</param>
        /// <param name="token">Current token buffer to resolve and clear.</param>
        /// <param name="lowerWordToCasing">Lowercased-word to canonical-casing mapping.</param>
        private static void _AppendResolvedToken(StringBuilder output, StringBuilder token, IReadOnlyDictionary<string, string> lowerWordToCasing)
        {
            if (token.Length == 0)
            {
                return;
            }

            var originalWord = token.ToString();
            var lowerWord = originalWord.ToLowerInvariant();
            // Unknown words are emitted unchanged.
            var resolvedWord = lowerWordToCasing.GetValueOrDefault(lowerWord, originalWord);
            output.Append(resolvedWord);
            token.Clear();
        }

        /// <summary>
        /// Uppercases the first letter of the text and of each sentence-start token.
        /// </summary>
        /// <param name="input">Input text to process.</param>
        /// <param name="wordSeparator">Configured word separator character.</param>
        /// <param name="sentenceEndChars">Characters treated as sentence boundaries.</param>
        /// <returns>Text with sentence starts uppercased.</returns>
        private static string _UppercaseSentenceInitials(string input, char wordSeparator, string sentenceEndChars)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var chars = input.ToCharArray();
            _UppercaseFirstAsciiLetter(chars, startIndex: 0);

            if (sentenceEndChars.Length == 0)
            {
                return new string(chars);
            }

            var sentenceEndToIsIncluded = new HashSet<char>(sentenceEndChars.Where(c => c != wordSeparator));
            if (sentenceEndToIsIncluded.Count == 0)
            {
                return new string(chars);
            }

            // Find each sentence-end marker and uppercase the next word's first letter.
            for (var i = 0; i < chars.Length; i++)
            {
                if (!sentenceEndToIsIncluded.Contains(chars[i]))
                {
                    continue;
                }

                var nextIndex = i + 1;
                while (nextIndex < chars.Length && chars[nextIndex] == wordSeparator)
                {
                    nextIndex++;
                }

                _UppercaseFirstAsciiLetter(chars, nextIndex);
            }

            return new string(chars);
        }

        /// <summary>
        /// Uppercases the first lowercase ASCII letter from the specified index.
        /// </summary>
        /// <param name="chars">Character buffer to mutate in place.</param>
        /// <param name="startIndex">Inclusive index where scanning begins.</param>
        private static void _UppercaseFirstAsciiLetter(char[] chars, int startIndex)
        {
            // Uppercase the first lowercase ASCII letter, or stop if the first letter is already uppercase.
            for (var i = startIndex; i < chars.Length; i++)
            {
                if (!char.IsAsciiLetterLower(chars[i]))
                {
                    if (char.IsAsciiLetterUpper(chars[i]))
                    {
                        return;
                    }

                    continue;
                }

                chars[i] = char.ToUpperInvariant(chars[i]);
                return;
            }
        }
    }
}
