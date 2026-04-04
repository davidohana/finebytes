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
    public sealed record FormatterFilter(
        bool Enabled,
        FilterTarget Target,
        FormatterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";
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
    public sealed record FixLeadingZerosFilter(
        bool Enabled,
        FilterTarget Target,
        FixLeadingZerosOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "FixLeadingZeros";
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
    public sealed record StripParenthesesFilter(
        bool Enabled,
        FilterTarget Target,
        StripParenthesesOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripParentheses";
    }
}
