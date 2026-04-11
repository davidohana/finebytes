using System.Text.RegularExpressions;
namespace Mfr.Models.Filters.Text
{
    /// <summary>
    /// Options for letter-case transformations.
    /// </summary>
    /// <param name="Mode">Case transformation mode.</param>
    /// <param name="SkipWords">Words to leave lowercased in title case mode.</param>
    public sealed record LettersCaseOptions(
        LettersCaseMode Mode,
        IReadOnlyList<string> SkipWords);

    /// <summary>
    /// Supported letter-case transformation modes.
    /// </summary>
    public enum LettersCaseMode
    {
        UpperCase,
        LowerCase,
        TitleCase,
        SentenceCase,
        InvertCase
    }

    /// <summary>
    /// Converts text letter casing.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Case transformation options.</param>
    public sealed partial record LettersCaseFilter(
        bool Enabled,
        FilterTarget Target,
        LettersCaseOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "LettersCase";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            return Options.Mode switch
            {
                LettersCaseMode.UpperCase => segment.ToUpperInvariant(),
                LettersCaseMode.LowerCase => segment.ToLowerInvariant(),
                LettersCaseMode.TitleCase => _ApplyTitleCase(segment, Options.SkipWords),
                LettersCaseMode.SentenceCase => _ApplySentenceCase(segment),
                LettersCaseMode.InvertCase => _InvertCase(segment),
                _ => segment
            };
        }

        private static string _ApplyTitleCase(string input, IReadOnlyList<string> skipWords)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var skipWordToIsExcluded = new HashSet<string>(skipWords, StringComparer.OrdinalIgnoreCase);
            return _WordRegex().Replace(input, m =>
            {
                var word = m.Value;
                return skipWordToIsExcluded.Contains(word)
                    ? word.ToLowerInvariant()
                    : word.Length == 1 ? word.ToUpperInvariant() : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
            });
        }

        private static string _ApplySentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var lower = input.ToLowerInvariant();
            return _SentenceCaseRegex().Replace(lower, m =>
            {
                var prefix = m.Groups[1].Value;
                var ch = m.Groups[2].Value;
                return prefix + ch.ToUpperInvariant();
            });
        }

        private static string _InvertCase(string input)
        {
            var chars = input.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }
                else if (char.IsLower(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                }
            }

            return new string(chars);
        }

        [GeneratedRegex(@"\b[0-9A-Za-z']+\b", RegexOptions.Compiled)]
        private static partial Regex _WordRegex();

        [GeneratedRegex(@"(^|[.!?]\s+)([a-z])", RegexOptions.Compiled)]
        private static partial Regex _SentenceCaseRegex();
    }
}
