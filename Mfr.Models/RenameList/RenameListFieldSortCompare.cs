using System.Globalization;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Typed ordering helpers for Rename List Auto-Sort comparisons.
    /// </summary>
    internal static class RenameListFieldSortCompare
    {
        /// <summary>
        /// Compares display strings with ordinal case-insensitive ordering.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="right">Right value.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int String(string left, string right)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(left, right);
        }

        /// <summary>
        /// Compares filesystem paths with the OS path comparer.
        /// </summary>
        /// <param name="left">Left path.</param>
        /// <param name="right">Right path.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int Path(string left, string right)
        {
            return PathComparers.Os.Compare(left, right);
        }

        /// <summary>
        /// Compares timestamps chronologically.
        /// </summary>
        /// <param name="left">Left timestamp.</param>
        /// <param name="right">Right timestamp.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int DateTime(DateTime left, DateTime right)
        {
            return left.CompareTo(right);
        }

        /// <summary>
        /// Compares 64-bit integers.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="right">Right value.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int Int64(long left, long right)
        {
            return left.CompareTo(right);
        }

        /// <summary>
        /// Compares 32-bit integers.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="right">Right value.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int Int32(int left, int right)
        {
            return left.CompareTo(right);
        }

        /// <summary>
        /// Compares durations.
        /// </summary>
        /// <param name="left">Left duration.</param>
        /// <param name="right">Right duration.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int TimeSpan(TimeSpan left, TimeSpan right)
        {
            return left.CompareTo(right);
        }

        /// <summary>
        /// Compares floating-point values.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="right">Right value.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int Double(double left, double right)
        {
            return left.CompareTo(right);
        }

        /// <summary>
        /// Compares invariant integer strings; non-numeric values sort as zero.
        /// </summary>
        /// <param name="left">Left formatted value.</param>
        /// <param name="right">Right formatted value.</param>
        /// <returns>Comparison sign for sort.</returns>
        internal static int ParsedInt64(string left, string right)
        {
            return Int64(_ParseInt64(left), _ParseInt64(right));
        }

        private static long _ParseInt64(string value)
        {
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }
    }
}
