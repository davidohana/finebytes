namespace Mfr.Filters
{
    /// <summary>
    /// Enforces the configured list-line maximum for embedded name, replace, and casing lists.
    /// </summary>
    internal static class ListEntryLength
    {
        /// <summary>
        /// Throws <see cref="UserException"/> when <paramref name="value"/> exceeds
        /// <c>ConfigStore.Config.Filters.MaxListFileLineLength</c>.
        /// </summary>
        /// <param name="value">Entry text to check.</param>
        /// <param name="messagePrefix">Leading message text before <c> exceeds maximum length (N).</c>.</param>
        internal static void ThrowIfTooLong(string value, string messagePrefix)
        {
            var maxLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            if (value.Length <= maxLen)
            {
                return;
            }

            throw new UserException($"{messagePrefix} exceeds maximum length ({maxLen}).");
        }
    }
}
