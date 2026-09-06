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
    /// Result of <see cref="FormatStringSyntax.TryValidate(string)"/>.
    /// </summary>
    /// <param name="Success"><see langword="true"/> when the template has no validation errors.</param>
    /// <param name="ErrorMessage">Failure message when <see cref="Success"/> is false; otherwise null.</param>
    /// <param name="ErrorPosition">Start of the failing span, or <c>-1</c> when OK.</param>
    /// <param name="ErrorLength">Length of the failing span, or <c>0</c> when OK.</param>
    /// <param name="Tokens">
    /// Validated token spans before any failure (empty when the walk fails or no tokens were accepted).
    /// </param>
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
    /// In <see cref="FormatStringValidationMode.Always"/>, closed <c>&lt;…&gt;</c> spans are validated with the
    /// same lookup and <see cref="Tokens.IFormatToken.Compile"/> rules as <see cref="FormatStringCompiler.Compile"/>
    /// (including names that do not look like tokens). Unclosed <c>&lt;</c> that looks like a token name is
    /// reported as an error; other unclosed angles remain literals (matching compile skip).
    /// </para>
    /// <para>
    /// In <see cref="FormatStringValidationMode.WhenLikelyTokens"/>, when
    /// <see cref="FormatStringCompiler.ContainsLikelyFormatTokens"/> is false the template is accepted as a
    /// literal (no spans). When it is true, validation matches <see cref="FormatStringValidationMode.Always"/>.
    /// </para>
    /// <para>
    /// On unknown-token or <see cref="Tokens.IFormatToken.Compile"/> failure, <see cref="FormatStringParseResult.Tokens"/>
    /// still lists spans validated before the failing piece (for live syntax highlight). Walk failures leave
    /// <see cref="FormatStringParseResult.Tokens"/> empty.
    /// </para>
    /// </remarks>
    public static class FormatStringSyntax
    {
        /// <summary>
        /// Validates <paramref name="template"/> with <see cref="FormatStringValidationMode.Always"/>.
        /// </summary>
        /// <param name="template">Template text that may contain formatter tokens.</param>
        /// <returns>Parse result with spans on success, or error position/length/message on failure.</returns>
        public static FormatStringParseResult TryValidate(string template)
        {
            return TryValidate(template, FormatStringValidationMode.Always);
        }

        /// <summary>
        /// Validates <paramref name="template"/> and returns token spans or the first error location.
        /// </summary>
        /// <param name="template">Template text that may contain formatter tokens.</param>
        /// <param name="mode">Whether to always validate or only when likely tokens are present.</param>
        /// <returns>Parse result with spans on success, or error position/length/message on failure.</returns>
        public static FormatStringParseResult TryValidate(string template, FormatStringValidationMode mode)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (
                mode == FormatStringValidationMode.WhenLikelyTokens
                && !FormatStringCompiler.ContainsLikelyFormatTokens(template)
            )
            {
                return new FormatStringParseResult(
                    Success: true,
                    ErrorMessage: null,
                    ErrorPosition: -1,
                    ErrorLength: 0,
                    Tokens: []
                );
            }

            var pieces = new List<FormatStringPiece>();
            if (!FormatStringScan.TryWalk(template, errorOnUnclosedLikelyToken: true, pieces, out var walkError))
            {
                return _Fail(walkError.Message, walkError.Position, walkError.Length);
            }

            var tokens = new List<FormatTokenSpan>();
            foreach (var piece in pieces)
            {
                if (!piece.IsToken)
                {
                    continue;
                }

                if (!FormatTokenRegistry.NameToToken.TryGetValue(piece.Name, out var token))
                {
                    return _Fail(FormatStringScan.UnknownTokenMessage(piece.Name), piece.Start, piece.Length, tokens);
                }

                try
                {
                    _ = token.Compile(piece.Args);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
                {
                    return _Fail(ex.Message, piece.Start, piece.Length, tokens);
                }

                tokens.Add(
                    new FormatTokenSpan(
                        Start: piece.Start,
                        Length: piece.Length,
                        CanonicalName: token.Names[0],
                        Args: piece.Args
                    )
                );
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
        /// Builds a failed parse result, optionally keeping spans validated before the failure.
        /// </summary>
        private static FormatStringParseResult _Fail(
            string message,
            int position,
            int length,
            IReadOnlyList<FormatTokenSpan>? tokens = null
        )
        {
            return new(
                Success: false,
                ErrorMessage: message,
                ErrorPosition: position,
                ErrorLength: length,
                Tokens: tokens ?? []
            );
        }
    }
}
