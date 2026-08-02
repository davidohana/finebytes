namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name-numeric-value&gt;</c> token to the first digit run in the preview prefix.
    /// </summary>
    /// <remarks>
    /// Leading zeros are stripped. When the prefix has no digits, the token expands to <c>0</c>.
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
            return item => _ExtractNumericValue(item.Preview.Prefix);
        }

        /// <summary>
        /// Returns the first contiguous ASCII digit run in <paramref name="prefix"/>, without leading zeros.
        /// </summary>
        /// <param name="prefix">Preview file name without extension.</param>
        /// <returns><c>0</c> when no digits are present; otherwise the digit run with leading zeros removed.</returns>
        private static string _ExtractNumericValue(string prefix)
        {
            for (var i = 0; i < prefix.Length; i++)
            {
                if (!char.IsAsciiDigit(prefix[i]))
                    continue;

                var end = i + 1;
                while (end < prefix.Length && char.IsAsciiDigit(prefix[end]))
                    end++;

                var digits = prefix.AsSpan(i, end - i).TrimStart('0');
                return digits.IsEmpty ? "0" : digits.ToString();
            }

            return "0";
        }
    }
}
