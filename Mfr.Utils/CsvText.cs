namespace Mfr.Utils
{
    /// <summary>
    /// RFC 4180 CSV field escaping and row formatting.
    /// </summary>
    public static class CsvText
    {
        /// <summary>
        /// Escapes a single CSV field: quotes when the value contains comma, quote, CR, or LF.
        /// </summary>
        /// <param name="value">Field text; <see langword="null"/> is treated as empty.</param>
        /// <returns>Escaped field suitable for a CSV cell (without surrounding commas).</returns>
        public static string EscapeField(string? value)
        {
            var text = value ?? string.Empty;
            var needsQuotes = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is ',' or '"' or '\r' or '\n')
                {
                    needsQuotes = true;
                    break;
                }
            }

            if (!needsQuotes)
            {
                return text;
            }

            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        /// <summary>
        /// Formats one CSV row (fields joined by commas; no trailing newline).
        /// </summary>
        /// <param name="fields">Field values in column order.</param>
        /// <returns>One CSV record line.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
        public static string FormatRow(IReadOnlyList<string> fields)
        {
            ArgumentNullException.ThrowIfNull(fields);

            if (fields.Count == 0)
            {
                return string.Empty;
            }

            if (fields.Count == 1)
            {
                return EscapeField(fields[0]);
            }

            return string.Join(',', fields.Select(EscapeField));
        }
    }
}
