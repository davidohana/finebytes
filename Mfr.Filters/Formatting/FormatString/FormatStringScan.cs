namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// Shared format-string span helpers used by <see cref="FormatStringCompiler"/> and <see cref="FormatStringSyntax"/>.
    /// </summary>
    internal static class FormatStringScan
    {
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
