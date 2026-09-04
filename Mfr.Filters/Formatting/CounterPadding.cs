using System.Globalization;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Shared leading-zero width and formatting for <see cref="CounterFilter"/> and the
    /// <c>&lt;counter&gt;</c> token (MFR7-style digit pad, sign-safe).
    /// </summary>
    internal static class CounterPadding
    {
        /// <summary>
        /// Digit width (excluding sign) so every value <c>start + step×i</c> for
        /// <c>i</c> in <c>0…maxIndex</c> fits when formatted with leading zeros.
        /// </summary>
        /// <param name="start">Counter value at index 0.</param>
        /// <param name="step">Increment per index.</param>
        /// <param name="maxIndex">Highest index in the active list scope.</param>
        /// <returns>Minimum zero-pad digit count (at least 1 when the range is non-empty).</returns>
        internal static int AutomaticDigitWidth(int start, int step, int maxIndex)
        {
            var v0 = start + ((long)step * 0);
            var v1 = start + ((long)step * maxIndex);
            return Math.Max(_DigitCount(v0), _DigitCount(v1));
        }

        /// <summary>
        /// Formats <paramref name="value"/> with at least <paramref name="digitWidth"/> zero-padded digits.
        /// </summary>
        /// <param name="value">Counter value.</param>
        /// <param name="digitWidth">
        /// Minimum digit count excluding the sign. When <c>&lt;= 0</c>, returns invariant with no padding.
        /// </param>
        /// <returns>Formatted counter text (sign before padded digits when negative).</returns>
        internal static string Format(long value, int digitWidth)
        {
            if (digitWidth <= 0)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            var format = new string('0', digitWidth);
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static int _DigitCount(long value)
        {
            return Math.Abs(value).ToString(CultureInfo.InvariantCulture).Length;
        }
    }
}
