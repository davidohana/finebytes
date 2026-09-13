namespace Mfr.Utils
{
    /// <summary>
    /// Splits multiline editor text into lines with stable newline handling.
    /// </summary>
    public static class MultilineText
    {
        /// <summary>
        /// Enumerates lines using the same rules as <see cref="StringReader.ReadLine"/>.
        /// </summary>
        /// <remarks>
        /// Accepts LF, CR, and CRLF. A trailing newline after the last non-empty line does not yield
        /// an extra empty line. Interior blank lines are yielded as empty strings. Null or empty
        /// text yields no lines.
        /// </remarks>
        /// <param name="text">Multiline editor text.</param>
        /// <returns>Lines in order, without newline characters.</returns>
        public static IEnumerable<string> EnumerateLines(string? text)
        {
            return ToLineList(text);
        }

        /// <summary>
        /// Splits <paramref name="text"/> into lines (same rules as <see cref="EnumerateLines"/>).
        /// </summary>
        /// <param name="text">Multiline editor text.</param>
        /// <returns>A list of lines; empty when <paramref name="text"/> is null or empty.</returns>
        public static IReadOnlyList<string> ToLineList(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return [];
            }

            var estimatedCount = 1;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    estimatedCount++;
                }
            }

            var lines = new List<string>(estimatedCount);
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is not ('\r' or '\n'))
                {
                    continue;
                }

                lines.Add(text[start..i]);
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }

            if (start < text.Length)
            {
                lines.Add(text[start..]);
            }

            return lines;
        }
    }
}
