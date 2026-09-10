namespace Mfr.Filters
{
    /// <summary>
    /// Enforces the list-line maximum for embedded name, replace, and casing lists.
    /// </summary>
    /// <remarks>
    /// Hosts sync <see cref="MaxLength"/> from process config via <see cref="Configure"/> after
    /// config load / CLI overrides. Filters never read <c>ConfigStore</c>.
    /// </remarks>
    public static class ListEntryLength
    {
        /// <summary>
        /// Default maximum, matching <c>MfrConfig.FilterConfig.MaxListFileLineLength</c>.
        /// </summary>
        public const int DefaultMaxLength = 1000;

        /// <summary>
        /// Current maximum list-entry length used at apply/setup when callers omit an explicit max.
        /// </summary>
        public static int MaxLength { get; private set; } = DefaultMaxLength;

        /// <summary>
        /// Sets the Filters-owned maximum list-entry length.
        /// </summary>
        /// <param name="maxListFileLineLength">Maximum characters per list entry (at least 1).</param>
        public static void Configure(int maxListFileLineLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxListFileLineLength, 1);
            MaxLength = maxListFileLineLength;
        }

        /// <summary>
        /// Throws <see cref="UserException"/> when <paramref name="value"/> exceeds
        /// <paramref name="maxLen"/>.
        /// </summary>
        /// <param name="value">Entry text to check.</param>
        /// <param name="messagePrefix">Leading message text before <c> exceeds maximum length (N).</c>.</param>
        /// <param name="maxLen">Maximum allowed length in characters.</param>
        internal static void ThrowIfTooLong(string value, string messagePrefix, int maxLen)
        {
            if (value.Length <= maxLen)
            {
                return;
            }

            throw new UserException($"{messagePrefix} exceeds maximum length ({maxLen}).");
        }
    }
}
