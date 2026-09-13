namespace Mfr.Filters
{
    /// <summary>
    /// Limits for embedded name, replace, and casing list entries (and their Filter Configuration editors).
    /// </summary>
    public static class ListEntryLength
    {
        /// <summary>
        /// Maximum characters per list entry (name line, casing word, or replace search/replacement).
        /// </summary>
        public const int DefaultMaxLength = 2000;

        /// <summary>
        /// Maximum number of entries in an embedded list.
        /// </summary>
        public const int DefaultMaxEntryCount = 10_000;

        /// <summary>
        /// Maximum characters in a list Filter Configuration text box (paste budget).
        /// </summary>
        public const int DefaultEditorTextMaxLength = 500_000;

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

        /// <summary>
        /// Throws <see cref="UserException"/> when <paramref name="count"/> exceeds
        /// <paramref name="maxCount"/>.
        /// </summary>
        /// <param name="count">Number of list entries.</param>
        /// <param name="listLabel">List name used in the error (e.g. <c>Name-list</c>).</param>
        /// <param name="maxCount">Maximum allowed entry count.</param>
        internal static void ThrowIfTooManyEntries(int count, string listLabel, int maxCount)
        {
            if (count <= maxCount)
            {
                return;
            }

            throw new UserException($"{listLabel} has too many entries (maximum {maxCount}).");
        }
    }
}
