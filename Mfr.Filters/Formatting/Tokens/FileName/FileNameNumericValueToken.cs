namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name-numeric-value&gt;</c> token to the first digit run in the preview full name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full name is preview prefix plus extension. Leading zeros are stripped. When the full name has no
    /// digits, the token expands to <c>0</c>.
    /// </para>
    /// </remarks>
    internal sealed class FileNameNumericValueToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name-numeric-value"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item =>
            {
                var preview = item.Preview;
                return _ExtractNumericValue(preview.Prefix + preview.Extension);
            };
        }

        /// <summary>
        /// Returns the first contiguous ASCII digit run in <paramref name="fullName"/>, without leading zeros.
        /// </summary>
        /// <param name="fullName">Preview file name including extension.</param>
        /// <returns><c>0</c> when no digits are present; otherwise the digit run with leading zeros removed.</returns>
        private static string _ExtractNumericValue(string fullName)
        {
            for (var i = 0; i < fullName.Length; i++)
            {
                if (!char.IsAsciiDigit(fullName[i]))
                    continue;

                var end = i + 1;
                while (end < fullName.Length && char.IsAsciiDigit(fullName[end]))
                    end++;

                var digits = fullName.AsSpan(i, end - i).TrimStart('0');
                return digits.IsEmpty ? "0" : digits.ToString();
            }

            return "0";
        }
    }
}
