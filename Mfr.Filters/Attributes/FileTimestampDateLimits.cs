namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Allowed calendar-date range for filesystem timestamp setters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower bound matches Windows FILETIME (<c>1601-01-01</c>). Upper bound is a product ceiling at
    /// <c>2100-12-31</c> so absurd far-future years (typos like <c>3026</c>) are rejected while
    /// still covering normal file-timestamp use (not the OS FILETIME max of 9999).
    /// </para>
    /// </remarks>
    public static class FileTimestampDateLimits
    {
        /// <summary>
        /// Earliest calendar date Windows <c>File.Set*Time</c> APIs accept (local).
        /// </summary>
        public static readonly DateOnly Min = new(1601, 1, 1);

        /// <summary>
        /// Latest calendar date accepted by Date/Time Setter (inclusive product ceiling).
        /// </summary>
        public static readonly DateOnly Max = new(2100, 12, 31);

        /// <summary>
        /// Returns whether <paramref name="date"/> is inside <see cref="Min"/>..<see cref="Max"/>.
        /// </summary>
        /// <param name="date">Candidate calendar date.</param>
        /// <returns>
        /// <see langword="true"/> when the date is inside the product-accepted setter range.
        /// </returns>
        public static bool IsInRange(DateOnly date)
        {
            return date >= Min && date <= Max;
        }

        /// <summary>
        /// Clamps <paramref name="value"/>'s calendar date into <see cref="Min"/>..<see cref="Max"/>.
        /// </summary>
        /// <param name="value">Candidate timestamp (time-of-day and <see cref="DateTime.Kind"/> are kept).</param>
        /// <returns>
        /// <paramref name="value"/> unchanged when in range; otherwise the nearer range endpoint with the same
        /// time-of-day and kind.
        /// </returns>
        public static DateTime Clamp(DateTime value)
        {
            var date = DateOnly.FromDateTime(value);
            if (IsInRange(date))
            {
                return value;
            }

            var limit = date < Min ? Min : Max;
            return limit.ToDateTime(TimeOnly.FromDateTime(value), value.Kind);
        }

        /// <summary>
        /// Builds a timestamp on <see cref="Min"/> or <see cref="Max"/> with the given time-of-day and kind.
        /// </summary>
        /// <param name="towardMax">
        /// When <see langword="true"/>, uses <see cref="Max"/>; otherwise <see cref="Min"/>.
        /// </param>
        /// <param name="time">Time-of-day to keep.</param>
        /// <param name="kind"><see cref="DateTime.Kind"/> for the result.</param>
        /// <returns>Endpoint date combined with <paramref name="time"/>.</returns>
        public static DateTime AtBound(bool towardMax, TimeOnly time, DateTimeKind kind)
        {
            var limit = towardMax ? Max : Min;
            return limit.ToDateTime(time, kind);
        }
    }
}
