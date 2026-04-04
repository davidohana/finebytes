using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mfr8.Models
{
    /// <summary>
    /// Matching mode for replacer patterns.
    /// </summary>
    public enum ReplacerMode
    {
        Literal,
        Wildcard,
        Regex
    }

    /// <summary>
    /// Options for replacer transformations.
    /// </summary>
    /// <param name="Find">Search pattern.</param>
    /// <param name="Replacement">Replacement value.</param>
    /// <param name="Mode">Pattern interpretation mode.</param>
    /// <param name="CaseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="ReplaceAll">Whether all matches are replaced.</param>
    /// <param name="WholeWord">Whether matching is constrained to whole words.</param>
    public sealed record ReplacerOptions(
        string Find,
        string Replacement,
        ReplacerMode Mode,
        bool CaseSensitive,
        bool ReplaceAll,
        bool WholeWord);

    /// <summary>
    /// Replaces text according to search options.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replacement options.</param>
    public sealed record ReplacerFilter(
        bool Enabled,
        FilterTarget Target,
        ReplacerOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Replacer";

        internal override string Apply(string segment, FileEntryLite file)
        {
            var regexOptions = Options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var pattern = Options.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(Options.Find),
                ReplacerMode.Wildcard => _WildcardToRegex(Options.Find),
                ReplacerMode.Regex => Options.Find,
                _ => Options.Find
            };

            if (Options.WholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            if (Options.ReplaceAll)
            {
                return Regex.Replace(segment, pattern, Options.Replacement, regexOptions);
            }

            var regex = new Regex(pattern, regexOptions);
            return regex.Replace(segment, Options.Replacement, 1);
        }

        private static string _WildcardToRegex(string wildcard)
        {
            var sb = new StringBuilder();
            foreach (var ch in wildcard)
            {
                _ = sb.Append(ch switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => Regex.Escape(ch.ToString())
                });
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Options for formatter templates.
    /// </summary>
    /// <param name="Template">Template expression with formatter tokens.</param>
    public sealed record FormatterOptions(string Template);

    /// <summary>
    /// Applies formatter template tokens.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Formatter options.</param>
    public sealed partial record FormatterFilter(
        bool Enabled,
        FilterTarget Target,
        FormatterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return _TokenRegex().Replace(Options.Template, m => _ResolveToken(m.Groups[1].Value, file));
        }

        private static string _ResolveToken(string tokenInner, FileEntryLite file)
        {
            var parts = tokenInner.Split(':', 2);
            var name = parts[0];
            var arg = parts.Length == 2 ? parts[1] : "";

            return name switch
            {
                "file-name" => file.Prefix,
                "file-ext" => file.Extension,
                "ext" => file.Extension,
                "full-name" => file.Prefix + file.Extension,
                "parent-folder" => Path.GetFileName(file.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                "full-path" => file.FullPath,
                "now" => string.IsNullOrWhiteSpace(arg) ? DateTimeOffset.UtcNow.ToString("o") : DateTimeOffset.UtcNow.ToString(arg),
                "counter" => _ResolveCounterToken(arg, file),
                _ => throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.")
            };
        }

        private static string _ResolveCounterToken(string arg, FileEntryLite file)
        {
            var parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");
            }

            var start = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var step = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var reset = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var width = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var pad = int.Parse(parts[4], CultureInfo.InvariantCulture);

            var n = reset == 1 ? file.FolderOccurrenceIndex : file.GlobalIndex;
            var value = start + ((long)step * n);
            var raw = value.ToString(CultureInfo.InvariantCulture);
            if (width <= 0)
            {
                return raw;
            }

            var padChar = pad == 0 ? '0' : ' ';
            return raw.PadLeft(width, padChar);
        }

        [GeneratedRegex(@"<([^<>]+)>", RegexOptions.Compiled)]
        private static partial Regex _TokenRegex();
    }

    /// <summary>
    /// Positioning mode for counter insertion.
    /// </summary>
    public enum CounterPosition
    {
        Prepend,
        Append,
        Replace
    }

    /// <summary>
    /// Options for counter generation and placement.
    /// </summary>
    /// <param name="Start">Counter start value.</param>
    /// <param name="Step">Counter increment step.</param>
    /// <param name="Width">Output width for padding.</param>
    /// <param name="PadChar">Pad character selector.</param>
    /// <param name="Position">Where to place the counter result.</param>
    /// <param name="Separator">Separator used for prepend/append mode.</param>
    /// <param name="ResetPerFolder">Whether to reset per folder.</param>
    public sealed record CounterOptions(
        int Start,
        int Step,
        int Width,
        string PadChar,
        CounterPosition Position,
        string Separator,
        bool ResetPerFolder);

    /// <summary>
    /// Injects generated counter values into a segment.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Counter options.</param>
    public sealed record CounterFilter(
        bool Enabled,
        FilterTarget Target,
        CounterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Counter";

        internal override string Apply(string segment, FileEntryLite file)
        {
            var n = Options.ResetPerFolder ? file.FolderOccurrenceIndex : file.GlobalIndex;
            var value = Options.Start + ((long)Options.Step * n);

            var pad = Options.PadChar switch
            {
                "0" => '0',
                "1" => ' ',
                _ => string.IsNullOrEmpty(Options.PadChar) ? '0' : Options.PadChar[0]
            };

            var raw = value.ToString(CultureInfo.InvariantCulture);
            var formatted = Options.Width > 0 ? raw.PadLeft(Options.Width, pad) : raw;

            return Options.Position switch
            {
                CounterPosition.Replace => formatted,
                CounterPosition.Prepend => formatted + Options.Separator + segment,
                CounterPosition.Append => segment + Options.Separator + formatted,
                _ => segment
            };
        }
    }

    /// <summary>
    /// Options for illegal/custom character cleanup.
    /// </summary>
    /// <param name="RemoveIllegalChars">Whether illegal file-name characters are removed/replaced.</param>
    /// <param name="IllegalCharReplacement">Replacement value for illegal characters.</param>
    /// <param name="CustomCharsToRemove">Custom characters to remove/replace.</param>
    /// <param name="CustomReplacement">Replacement value for custom characters.</param>
    public sealed record CleanerOptions(
        bool RemoveIllegalChars,
        string IllegalCharReplacement,
        string CustomCharsToRemove,
        string CustomReplacement);

    /// <summary>
    /// Cleans illegal and custom characters.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Cleaner options.</param>
    public sealed record CleanerFilter(
        bool Enabled,
        FilterTarget Target,
        CleanerOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Cleaner";

        internal override string Apply(string segment, FileEntryLite file)
        {
            var res = segment;
            if (Options.RemoveIllegalChars)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    res = res.Replace(c.ToString(), Options.IllegalCharReplacement);
                }
            }

            if (!string.IsNullOrEmpty(Options.CustomCharsToRemove))
            {
                foreach (var c in Options.CustomCharsToRemove)
                {
                    res = res.Replace(c.ToString(), Options.CustomReplacement);
                }
            }

            return res;
        }
    }

    /// <summary>
    /// Options for normalizing numeric leading zeros.
    /// </summary>
    /// <param name="Width">Target numeric width.</param>
    /// <param name="RemoveExtraZeros">Whether extra leading zeros are removed before padding.</param>
    public sealed record FixLeadingZerosOptions(
        int Width,
        bool RemoveExtraZeros);

    /// <summary>
    /// Normalizes leading zeros in numeric sequences.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Leading-zero normalization options.</param>
    public sealed partial record FixLeadingZerosFilter(
        bool Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "FixLeadingZeros";

        internal override string Apply(string segment, FileEntryLite file)
        {
            return Options.Width <= 0
                ? segment
                : _DigitsRegex().Replace(segment, m =>
                {
                    var digits = m.Value;
                    if (Options.RemoveExtraZeros)
                    {
                        digits = digits.TrimStart('0');
                    }

                    if (digits.Length == 0)
                    {
                        digits = "0";
                    }

                    return digits.PadLeft(Options.Width, '0');
                });
        }

        [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
        private static partial Regex _DigitsRegex();
    }

    /// <summary>
    /// Options for stripping bracket/parenthesis pairs.
    /// </summary>
    /// <param name="Types">Pipe-separated pair types to target.</param>
    /// <param name="RemoveContents">Whether to remove bracketed contents or only delimiters.</param>
    public sealed record StripParenthesesOptions(
        string Types,
        bool RemoveContents);

    /// <summary>
    /// Removes selected parenthesis/bracket delimiters and optionally their contents.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Parenthesis-strip options.</param>
    public sealed partial record StripParenthesesFilter(
        bool Enabled,
        FilterTarget Target,
        StripParenthesesOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripParentheses";

        internal override string Apply(string segment, FileEntryLite file)
        {
            var pairs = new List<(char open, char close)>();
            foreach (var token in Options.Types.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                switch (token.Trim())
                {
                    case "Round":
                        pairs.Add(('(', ')'));
                        break;
                    case "Square":
                        pairs.Add(('[', ']'));
                        break;
                    case "Curly":
                        pairs.Add(('{', '}'));
                        break;
                    case "Angle":
                        pairs.Add(('<', '>'));
                        break;
                    default:
                        break;
                }
            }

            var res = segment;
            foreach ((var open, var close) in pairs)
            {
                if (open == '(' && close == ')')
                {
                    res = Options.RemoveContents
                        ? _RoundParenRegex().Replace(res, "")
                        : res.Replace("(", "").Replace(")", "");
                }
                else if (open == '[' && close == ']')
                {
                    res = Options.RemoveContents
                        ? _SquareParenRegex().Replace(res, "")
                        : res.Replace("[", "").Replace("]", "");
                }
                else if (open == '{' && close == '}')
                {
                    res = Options.RemoveContents
                        ? _CurlyParenRegex().Replace(res, "")
                        : res.Replace("{", "").Replace("}", "");
                }
                else if (open == '<' && close == '>')
                {
                    res = Options.RemoveContents
                        ? _AngleParenRegex().Replace(res, "")
                        : res.Replace("<", "").Replace(">", "");
                }
            }

            return res;
        }

        [GeneratedRegex(@"\([^)]*\)", RegexOptions.Compiled)]
        private static partial Regex _RoundParenRegex();

        [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
        private static partial Regex _SquareParenRegex();

        [GeneratedRegex(@"\{[^}]*\}", RegexOptions.Compiled)]
        private static partial Regex _CurlyParenRegex();

        [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
        private static partial Regex _AngleParenRegex();
    }
}
