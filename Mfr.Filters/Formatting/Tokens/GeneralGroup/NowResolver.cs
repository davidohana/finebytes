using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Resolves the <c>&lt;now&gt;</c> and <c>&lt;now:format&gt;</c> tokens.
    /// </summary>
    internal static class NowResolver
    {
        /// <summary>
        /// Returns the current UTC time formatted as ISO 8601 by default, or with the supplied format.
        /// </summary>
        /// <param name="arg">Optional .NET date/time format string.</param>
        /// <param name="item">Rename item (unused; kept for resolver signature uniformity).</param>
        /// <returns>The formatted current UTC time string.</returns>
        public static string Resolve(string arg, RenameItem item)
        {
            return string.IsNullOrWhiteSpace(arg)
                ? DateTimeOffset.UtcNow.ToString("o")
                : DateTimeOffset.UtcNow.ToString(arg);
        }
    }
}
