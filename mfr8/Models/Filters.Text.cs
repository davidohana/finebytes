using System.Text.RegularExpressions;

namespace Mfr8.Models
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

        internal override string Apply(string segment, FileEntryLite file)
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

            var skip = new HashSet<string>(skipWords, StringComparer.OrdinalIgnoreCase);
            return _WordRegex().Replace(input, m =>
            {
                var word = m.Value;
                return skip.Contains(word)
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

    /// <summary>
    /// Options for replacing spaces and replacing a chosen character with spaces.
    /// </summary>
    /// <param name="ReplaceSpaceWith">Replacement text for spaces.</param>
    /// <param name="ReplaceCharWithSpace">Character/text to replace with a space.</param>
    public sealed record SpaceCharacterOptions(
        string ReplaceSpaceWith,
        string ReplaceCharWithSpace);

    /// <summary>
    /// Replaces spaces and mapped characters.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Space replacement options.</param>
    public sealed record SpaceCharacterFilter(
        bool Enabled,
        FilterTarget Target,
        SpaceCharacterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceCharacter";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return segment.Replace(" ", Options.ReplaceSpaceWith).Replace(Options.ReplaceCharWithSpace, " ");
        }
    }

    /// <summary>
    /// Removes all whitespace from the target segment.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record RemoveSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "RemoveSpaces";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return TextFilterRegexCache.WhitespaceRegex.Replace(segment, "");
        }
    }

    /// <summary>
    /// Collapses runs of whitespace into single spaces.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record ShrinkSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkSpaces";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return TextFilterRegexCache.WhitespaceRegex.Replace(segment, " ");
        }
    }

    /// <summary>
    /// Trims a fixed number of characters from the left.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimLeftFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimLeft";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Count <= 0 ? segment : segment.Length <= Options.Count ? "" : segment[Options.Count..];
        }
    }

    /// <summary>
    /// Trims a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimRight";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Count <= 0 ? segment : segment.Length <= Options.Count ? "" : segment[..^Options.Count];
        }
    }

    /// <summary>
    /// Extracts a fixed number of characters from the left.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractLeftFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractLeft";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Count <= 0 ? "" : segment.Length <= Options.Count ? segment : segment[..Options.Count];
        }
    }

    /// <summary>
    /// Extracts a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Count <= 0 ? "" : segment.Length <= Options.Count ? segment : segment[^Options.Count..];
        }
    }

    internal static partial class TextFilterRegexCache
    {
        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex _WhitespaceRegex();

        internal static Regex WhitespaceRegex => _WhitespaceRegex();
    }
}
