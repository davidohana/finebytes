namespace Mfr.Filters.Misc
{
    /// <summary>
    /// Parenthesis/bracket types.
    /// </summary>
    public enum ParenthesisType
    {
        Round,
        Square,
        Curly,
        Angle,
    }

    /// <summary>
    /// Options for stripping bracket/parenthesis pairs.
    /// </summary>
    /// <param name="Type">Pair type to target.</param>
    /// <param name="RemoveContents">Whether to remove bracketed contents or only delimiters.</param>
    public sealed record StripParenthesesOptions(ParenthesisType Type, bool RemoveContents);

    /// <summary>
    /// Removes selected parenthesis/bracket delimiters and optionally their contents.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Parenthesis-strip options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Misc, "Strip Parentheses")]
    public sealed record StripParenthesesFilter(
        FilterTarget Target,
        StripParenthesesOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, round parentheses, remove contents).
        /// </summary>
        public StripParenthesesFilter()
            : this(
                new FilePrefixTarget(),
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true)
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripParentheses";

        protected override string _TransformValue(string value, RenameItem item)
        {
            var (open, close) = Options.Type switch
            {
                ParenthesisType.Round => ('(', ')'),
                ParenthesisType.Square => ('[', ']'),
                ParenthesisType.Curly => ('{', '}'),
                ParenthesisType.Angle => ('<', '>'),
                _ => ('\0', '\0'),
            };

            if (open == '\0')
            {
                return value;
            }

            return _StripPairs(value, open, close, Options.RemoveContents);
        }

        /// <summary>
        /// Strips matched open/close pairs innermost-first (MFR7 StripParFilter), leaving unmatched delimiters.
        /// </summary>
        /// <param name="value">Input string.</param>
        /// <param name="open">Opening delimiter.</param>
        /// <param name="close">Closing delimiter.</param>
        /// <param name="removeContents">When true, remove delimiters and interior; otherwise remove delimiters only.</param>
        /// <returns>The stripped string.</returns>
        private static string _StripPairs(string value, char open, char close, bool removeContents)
        {
            var endPos = 0;
            while (true)
            {
                endPos = value.IndexOf(close, endPos);
                if (endPos < 0)
                {
                    break;
                }

                var startPos = -1;
                for (var i = endPos - 1; i >= 0; i--)
                {
                    if (value[i] != open)
                    {
                        continue;
                    }

                    startPos = i;
                    break;
                }

                if (startPos < 0)
                {
                    endPos += 1;
                    continue;
                }

                if (removeContents)
                {
                    value = value.Remove(startPos, endPos - startPos + 1);
                    endPos = startPos;
                    continue;
                }

                // Remove close first (higher index), then open.
                value = value.Remove(endPos, 1).Remove(startPos, 1);
                endPos = startPos;
            }

            return value;
        }
    }
}
