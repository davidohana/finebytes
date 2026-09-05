namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Allowed calendar-date range for filesystem timestamp setters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower bound matches Windows FILETIME (<c>1601-01-01</c>). Upper bound is capped at
    /// <c>2100-12-31</c> so absurd far-future years (typos like <c>3026</c>) are rejected while
    /// still covering normal file-timestamp use.
    /// </para>
    /// </remarks>
    public static class FileTimestampDateLimits
    {
        /// <summary>
        /// Earliest calendar date Windows <c>File.Set*Time</c> APIs accept (local).
        /// </summary>
        public static readonly DateOnly Min = new(1601, 1, 1);

        /// <summary>
        /// Latest calendar date accepted by Date/Time Setter (inclusive).
        /// </summary>
        public static readonly DateOnly Max = new(2100, 12, 31);

        /// <summary>
        /// Returns whether <paramref name="date"/> is inside <see cref="Min"/>..<see cref="Max"/>.
        /// </summary>
        /// <param name="date">Candidate calendar date.</param>
        /// <returns><see langword="true"/> when the date may be written to a filesystem timestamp.</returns>
        public static bool IsInRange(DateOnly date)
        {
            return date >= Min && date <= Max;
        }
    }
}
