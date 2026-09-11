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
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            using var reader = new StringReader(text);
            while (reader.ReadLine() is { } line)
            {
                yield return line;
            }
        }
    }
}
