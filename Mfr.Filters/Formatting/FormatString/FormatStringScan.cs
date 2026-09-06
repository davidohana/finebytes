namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// One piece from a format-template walk: a literal run or a balanced <c>&lt;…&gt;</c> token.
    /// </summary>
    /// <param name="IsToken"><see langword="true"/> when this is a balanced token span.</param>
    /// <param name="Start">Zero-based start index in the template.</param>
    /// <param name="Length">Length of the piece in the template.</param>
    /// <param name="Name">Token name when <see cref="IsToken"/>; otherwise empty.</param>
    /// <param name="Args">Token args when <see cref="IsToken"/>; otherwise empty.</param>
    internal readonly record struct FormatStringPiece(
        bool IsToken,
        int Start,
        int Length,
        string Name,
        string Args
    );

    /// <summary>
    /// Structural error from <see cref="FormatStringScan.TryWalk"/> (unclosed likely token).
    /// </summary>
    /// <param name="Message">Human-readable error text.</param>
    /// <param name="Position">Start index of the failing span.</param>
    /// <param name="Length">Length of the failing span.</param>
    internal readonly record struct FormatStringWalkError(string Message, int Position, int Length);

    /// <summary>
    /// Shared format-string span helpers used by <see cref="FormatStringCompiler"/> and <see cref="FormatStringSyntax"/>.
    /// </summary>
    internal static class FormatStringScan
    {
        /// <summary>
        /// Message for an unknown token name (shared by Compile and TryValidate).
        /// </summary>
        /// <param name="name">Token name as parsed (before registry lookup).</param>
        /// <returns>Standard unknown-token error text.</returns>
        internal static string UnknownTokenMessage(string name)
        {
            return $"Unknown formatter token '<{name}>'. See the Formatter docs for supported tokens.";
        }

        /// <summary>
        /// Walks <paramref name="template"/> into ordered literal and balanced-token pieces.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unclosed <c>&lt;</c> that does not look like a token name is left in the literal stream (same as
        /// Compile). When <paramref name="errorOnUnclosedLikelyToken"/> is true, an unclosed angle whose
        /// name candidate passes <see cref="LooksLikeFormatterTokenName"/> fails the walk.
        /// </para>
        /// </remarks>
        /// <param name="template">Template text.</param>
        /// <param name="errorOnUnclosedLikelyToken">Whether unclosed likely token names are errors.</param>
        /// <param name="pieces">Receives ordered pieces on success (cleared first).</param>
        /// <param name="error">Set when the walk fails; otherwise default.</param>
        /// <returns><see langword="true"/> when the walk completed without structural error.</returns>
        internal static bool TryWalk(
            string template,
            bool errorOnUnclosedLikelyToken,
            List<FormatStringPiece> pieces,
            out FormatStringWalkError error
        )
        {
            pieces.Clear();
            error = default;

            var i = 0;
            var literalStart = 0;

            while (i < template.Length)
            {
                if (template[i] != '<')
                {
                    i++;
                    continue;
                }

                var tokenStart = i;
                var tokenEnd = FindMatchingClose(template, tokenStart);
                if (tokenEnd < 0)
                {
                    if (errorOnUnclosedLikelyToken)
                    {
                        var candidate = ExtractUnclosedNameCandidate(template, tokenStart);
                        if (LooksLikeFormatterTokenName(candidate))
                        {
                            error = new FormatStringWalkError(
                                Message: $"Unclosed formatter token starting at '<{candidate}'.",
                                Position: tokenStart,
                                Length: template.Length - tokenStart
                            );
                            pieces.Clear();
                            return false;
                        }
                    }

                    i++;
                    continue;
                }

                if (tokenStart > literalStart)
                {
                    pieces.Add(
                        new FormatStringPiece(
                            IsToken: false,
                            Start: literalStart,
                            Length: tokenStart - literalStart,
                            Name: "",
                            Args: ""
                        )
                    );
                }

                var tokenInner = template[(tokenStart + 1)..tokenEnd];
                SplitNameAndArgs(tokenInner, out var name, out var args);
                pieces.Add(
                    new FormatStringPiece(
                        IsToken: true,
                        Start: tokenStart,
                        Length: tokenEnd - tokenStart + 1,
                        Name: name,
                        Args: args
                    )
                );
                i = tokenEnd + 1;
                literalStart = i;
            }

            if (literalStart < template.Length)
            {
                pieces.Add(
                    new FormatStringPiece(
                        IsToken: false,
                        Start: literalStart,
                        Length: template.Length - literalStart,
                        Name: "",
                        Args: ""
                    )
                );
            }

            return true;
        }

        /// <summary>
        /// Scans forward from the opening <c>&lt;</c> at <paramref name="openIndex"/> and returns the
        /// index of the matching closing <c>&gt;</c>, or <c>-1</c> when no balanced close is found.
        /// </summary>
        /// <param name="template">Template text.</param>
        /// <param name="openIndex">Index of the opening <c>&lt;</c>.</param>
        /// <returns>Index of the matching <c>&gt;</c>, or <c>-1</c>.</returns>
        internal static int FindMatchingClose(string template, int openIndex)
        {
            var depth = 0;
            for (var j = openIndex; j < template.Length; j++)
            {
                if (template[j] == '<')
                {
                    depth++;
                }
                else if (template[j] == '>')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return j;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns whether <paramref name="name"/> matches the formatter token-name heuristic
        /// (ASCII letter, then letters/digits/<c>-</c>/<c>_</c>, at least two characters).
        /// </summary>
        /// <param name="name">Candidate token name (no args).</param>
        /// <returns><see langword="true"/> when the name looks like a formatter token.</returns>
        internal static bool LooksLikeFormatterTokenName(string name)
        {
            if (name.Length < 2 || !char.IsAsciiLetter(name[0]))
            {
                return false;
            }

            for (var k = 1; k < name.Length; k++)
            {
                var c = name[k];
                if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Splits a token inner span into name and args at the first <c>:</c>.
        /// </summary>
        /// <param name="tokenInner">Text between <c>&lt;</c> and <c>&gt;</c>.</param>
        /// <param name="name">Token name (before first <c>:</c>).</param>
        /// <param name="args">Argument text after the first <c>:</c>, or empty.</param>
        internal static void SplitNameAndArgs(string tokenInner, out string name, out string args)
        {
            var colonIndex = tokenInner.IndexOf(':');
            if (colonIndex < 0)
            {
                name = tokenInner;
                args = "";
                return;
            }

            name = tokenInner[..colonIndex];
            args = tokenInner[(colonIndex + 1)..];
        }

        /// <summary>
        /// Extracts a candidate token name after an unclosed <c>&lt;</c> for validation heuristics.
        /// </summary>
        /// <param name="template">Template text.</param>
        /// <param name="openIndex">Index of the opening <c>&lt;</c>.</param>
        /// <returns>Trimmed name-like prefix, or empty when none.</returns>
        internal static string ExtractUnclosedNameCandidate(string template, int openIndex)
        {
            var start = openIndex + 1;
            if (start >= template.Length)
            {
                return "";
            }

            var end = start;
            while (end < template.Length)
            {
                var c = template[end];
                if (c is ':' or '>' or '<' or ' ')
                {
                    break;
                }

                end++;
            }

            return template[start..end].Trim();
        }
    }
}
