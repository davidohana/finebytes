namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// One validated token span inside a format template.
    /// </summary>
    /// <param name="Start">Zero-based start index of the <c>&lt;</c>.</param>
    /// <param name="Length">Length of the full <c>&lt;…&gt;</c> span.</param>
    /// <param name="CanonicalName">Resolved primary token name (not an alias).</param>
    /// <param name="Args">Raw argument text after the first <c>:</c>, or empty.</param>
    public sealed record FormatTokenSpan(int Start, int Length, string CanonicalName, string Args);

    /// <summary>
    /// Result of <see cref="FormatStringSyntax.TryValidate"/>.
    /// </summary>
    /// <param name="Success"><see langword="true"/> when the template has no validation errors.</param>
    /// <param name="ErrorMessage">Failure message when <see cref="Success"/> is false; otherwise null.</param>
    /// <param name="ErrorPosition">Start of the failing span, or <c>-1</c> when OK.</param>
    /// <param name="ErrorLength">Length of the failing span, or <c>0</c> when OK.</param>
    /// <param name="Tokens">Successfully validated token spans (empty on failure).</param>
    public sealed record FormatStringParseResult(
        bool Success,
        string? ErrorMessage,
        int ErrorPosition,
        int ErrorLength,
        IReadOnlyList<FormatTokenSpan> Tokens
    );

    /// <summary>
    /// Public format-string validation for FormatEditor (spans + error location).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Closed <c>&lt;…&gt;</c> spans are validated with the same lookup and <see cref="Tokens.IFormatToken.Compile"/>
    /// rules as <see cref="FormatStringCompiler.Compile"/> (including names that do not look like tokens).
    /// Unclosed <c>&lt;</c> that looks like a token name is reported as an error; other unclosed angles remain
    /// literals (matching compile skip). The name heuristic matches
    /// <see cref="FormatStringCompiler.ContainsLikelyFormatTokens"/>.
    /// </para>
    /// </remarks>
    public static class FormatStringSyntax
    {
        /// <summary>
        /// Validates <paramref name="template"/> and returns token spans or the first error location.
        /// </summary>
        /// <param name="template">Template text that may contain formatter tokens.</param>
        /// <returns>Parse result with spans on success, or error position/length/message on failure.</returns>
        public static FormatStringParseResult TryValidate(string template)
        {
            ArgumentNullException.ThrowIfNull(template);

            var tokens = new List<FormatTokenSpan>();
            var i = 0;

            while (i < template.Length)
            {
                if (template[i] != '<')
                {
                    i++;
                    continue;
                }

                var tokenStart = i;
                var tokenEnd = FormatStringScan.FindMatchingClose(template, tokenStart);
                if (tokenEnd < 0)
                {
                    var candidate = FormatStringScan.ExtractUnclosedNameCandidate(template, tokenStart);
                    if (FormatStringScan.LooksLikeFormatterTokenName(candidate))
                    {
                        return _Fail(
                            $"Unclosed formatter token starting at '<{candidate}'.",
                            tokenStart,
                            template.Length - tokenStart
                        );
                    }

                    i++;
                    continue;
                }

                var tokenInner = template[(tokenStart + 1)..tokenEnd];
                FormatStringScan.SplitNameAndArgs(tokenInner, out var name, out var args);
                var spanLength = tokenEnd - tokenStart + 1;
                if (!FormatTokenRegistry.NameToToken.TryGetValue(name, out var token))
                {
                    return _Fail(
                        $"Unknown formatter token '<{name}>'. See the Formatter docs for supported tokens.",
                        tokenStart,
                        spanLength
                    );
                }

                try
                {
                    _ = token.Compile(args);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
                {
                    return _Fail(ex.Message, tokenStart, spanLength);
                }

                tokens.Add(
                    new FormatTokenSpan(
                        Start: tokenStart,
                        Length: spanLength,
                        CanonicalName: token.Names[0],
                        Args: args
                    )
                );
                i = tokenEnd + 1;
            }

            return new FormatStringParseResult(
                Success: true,
                ErrorMessage: null,
                ErrorPosition: -1,
                ErrorLength: 0,
                Tokens: tokens
            );
        }

        /// <summary>
        /// Builds a failed parse result with an empty token list.
        /// </summary>
        private static FormatStringParseResult _Fail(string message, int position, int length)
        {
            return new(Success: false, ErrorMessage: message, ErrorPosition: position, ErrorLength: length, Tokens: []);
        }
    }
}
