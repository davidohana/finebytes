using System.Globalization;

namespace Mfr.Utils
{
    /// <summary>
    /// Extracts the first numeric run from a full file name (MFR7 <c>GetFileNameNumeric</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matches at most 10 ASCII digits (regex <c>[0-9]{1,10}</c>), then parses as <see cref="long"/>.
    /// Leading zeros are stripped by the parse. When no digit run exists, the result is <c>0</c>.
    /// Shared by Rename List File Name Numeric Value and the formatter token.
    /// </para>
    /// </remarks>
    public static class FileNameNumericValue
    {
        /// <summary>
        /// Returns the first digit run in <paramref name="fullFileName"/> as a decimal string.
        /// </summary>
        /// <param name="fullFileName">File name including extension (no directory).</param>
        /// <returns><c>0</c> when no digits are present; otherwise the parsed numeric value.</returns>
        public static string Extract(string fullFileName)
        {
            ArgumentNullException.ThrowIfNull(fullFileName);

            for (var i = 0; i < fullFileName.Length; i++)
            {
                if (!char.IsAsciiDigit(fullFileName[i]))
                {
                    continue;
                }

                var end = i + 1;
                while (end < fullFileName.Length && char.IsAsciiDigit(fullFileName[end]) && end - i < 10)
                {
                    end++;
                }

                return long.Parse(fullFileName.AsSpan(i, end - i), CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
            }

            return "0";
        }
    }
}
