using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for casing-list based word casing.
    /// </summary>
    /// <param name="FilePath">Path to the casing-list text file (one word per line).</param>
    /// <param name="UppercaseSentenceInitial">
    /// When <c>true</c>, uppercases the first letter at string start and after sentence-end boundaries
    /// (see <see cref="RenameItem.SentenceEndChars"/>, set by <c>SentenceEndCharacters</c> when used earlier
    /// in the chain; default <c>".!?"</c>).
    /// </param>
    public sealed record CasingListOptions(
        string FilePath,
        bool UppercaseSentenceInitial = false);

    /// <summary>
    /// Changes each word's casing to match how it appears in a casing-list file.
    /// <para>
    /// Words not found in the list are left unchanged.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Casing-list options.</param>
    public sealed record CasingListFilter(
        FilterTarget Target,
        CasingListOptions Options) : FileNameSegmentFilter(Target)
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

            var transformed = _ApplyCasingList(segment, item.WordSeparator, lowerWordToCasing);
            if (!Options.UppercaseSentenceInitial)
            {
                return transformed;
            }

            return _UppercaseSentenceInitials(
                input: transformed,
                wordSeparator: item.WordSeparator,
                sentenceEndChars: item.SentenceEndChars);
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
            ReadOnlySpan<char> remaining = input;
            var output = new StringBuilder(input.Length);
            while (!remaining.IsEmpty)
            {
                var sep = remaining.IndexOf(wordSeparator);
                if (sep < 0)
                {
                    _AppendResolvedWord(output, remaining, lowerWordToCasing);
                    break;
                }

                _AppendResolvedWord(output, remaining[..sep], lowerWordToCasing);
                output.Append(wordSeparator);
                remaining = remaining[(sep + 1)..];
            }

            return output.ToString();
        }

        /// <summary>
        /// Appends one token using list casing when a match exists; unknown words are unchanged.
        /// </summary>
        /// <param name="output">Destination text builder.</param>
        /// <param name="word">Token text (may be empty between consecutive separators).</param>
        /// <param name="lowerWordToCasing">Lowercased-word to canonical-casing mapping.</param>
        private static void _AppendResolvedWord(
            StringBuilder output,
            ReadOnlySpan<char> word,
            IReadOnlyDictionary<string, string> lowerWordToCasing)
        {
            if (word.IsEmpty)
            {
                return;
            }

            var originalWord = word.ToString();
            var lowerWord = originalWord.ToLowerInvariant();
            output.Append(lowerWordToCasing.GetValueOrDefault(lowerWord, originalWord));
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
        /// Uppercases the first lowercase ASCII letter from the specified index, or stops at the first uppercase letter.
        /// </summary>
        /// <param name="chars">Character buffer to mutate in place.</param>
        /// <param name="startIndex">Inclusive index where scanning begins.</param>
        private static void _UppercaseFirstAsciiLetter(char[] chars, int startIndex)
        {
            for (var i = startIndex; i < chars.Length; i++)
            {
                if (char.IsAsciiLetterLower(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                    return;
                }

                if (char.IsAsciiLetterUpper(chars[i]))
                {
                    return;
                }
            }
        }
    }
}
