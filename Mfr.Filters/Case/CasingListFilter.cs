using System.Text;
using Mfr.Utils;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for casing-list based word casing.
    /// </summary>
    /// <param name="Words">
    /// Words to apply by exact spelling (case-insensitive match). Empty list is a no-op.
    /// </param>
    /// <param name="UppercaseSentenceInitial">
    /// When <c>true</c>, uppercases sentence starts via the same rules as Letters Case sentence mode
    /// (first letter, and after <see cref="RenameItem.SentenceEndChars"/> when followed by the word separator;
    /// default ends <c>".!?"</c>). Does not lowercase the rest of the text.
    /// </param>
    public sealed record CasingListOptions(IReadOnlyList<string> Words, bool UppercaseSentenceInitial = false);

    /// <summary>
    /// Changes each word's casing to match how it appears in the configured word list.
    /// <para>
    /// Words not found in the list are left unchanged.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Casing-list options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Case, "Casing List")]
    public sealed record CasingListFilter(
        FilterTarget Target,
        CasingListOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        private Dictionary<string, string>? _lowerWordToCasing;

        /// <summary>
        /// Creates a filter with add-to-list defaults (file prefix, empty word list, sentence-initial uppercasing).
        /// </summary>
        public CasingListFilter()
            : this(new FilePrefixTarget(), new CasingListOptions(Words: [], UppercaseSentenceInitial: true)) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "CasingList";

        /// <summary>
        /// Builds and caches the casing-list map for this filter instance.
        /// </summary>
        protected override void _Setup()
        {
            _lowerWordToCasing = CasingListParser.BuildMap(Options.Words);
        }

        /// <summary>
        /// Applies casing-list normalization and optional sentence-initial uppercasing.
        /// </summary>
        /// <param name="value">Input text segment to transform.</param>
        /// <param name="item">Current rename item carrying separator context.</param>
        /// <returns>The transformed text segment.</returns>
        protected override string _TransformValue(string value, RenameItem item)
        {
            var lowerWordToCasing = Check.NotNull(
                _lowerWordToCasing,
                "Casing-list setup must complete before transform."
            );

            if (value.Length == 0)
            {
                return value;
            }

            var transformed =
                lowerWordToCasing.Count == 0 ? value : _ApplyCasingList(value, item.WordSeparator, lowerWordToCasing);
            if (!Options.UppercaseSentenceInitial)
            {
                return transformed;
            }

            return SentenceInitialCasing.UppercaseInitials(
                input: transformed,
                wordSeparator: item.WordSeparator,
                sentenceEndChars: item.SentenceEndChars
            );
        }

        /// <summary>
        /// Rewrites each separator-delimited token using the casing-list dictionary.
        /// </summary>
        /// <param name="input">Input text to process.</param>
        /// <param name="wordSeparator">Configured word separator character.</param>
        /// <param name="lowerWordToCasing">Lowercased-word to canonical-casing mapping.</param>
        /// <returns>Text with matched words replaced by canonical casing.</returns>
        private static string _ApplyCasingList(
            string input,
            char wordSeparator,
            IReadOnlyDictionary<string, string> lowerWordToCasing
        )
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
            IReadOnlyDictionary<string, string> lowerWordToCasing
        )
        {
            if (word.IsEmpty)
            {
                return;
            }

            var originalWord = word.ToString();
            var lowerWord = originalWord.ToLowerInvariant();
            output.Append(lowerWordToCasing.GetValueOrDefault(lowerWord, originalWord));
        }
    }
}
