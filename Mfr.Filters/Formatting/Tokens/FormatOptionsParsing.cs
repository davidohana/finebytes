using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Shared helpers used by multiple formatter tokens (display labels, keyword hints, and common preconditions).
    /// </summary>
    internal static class FormatOptionsParsing
    {
        /// <summary>
        /// Formats keyword strings as a short English list for error messages (<c>x or y</c>; <c>x, y, or z</c>).
        /// </summary>
        /// <param name="keywords">Keywords to list (for example dictionary keys; insertion order is preserved).</param>
        /// <returns>A phrase suitable after <c>expected</c> in a user-facing message.</returns>
        internal static string FormatExpectedKeywords(IEnumerable<string> keywords)
        {
            var keys = keywords.ToArray();
            return keys.Length switch
            {
                0 => "",
                1 => keys[0],
                2 => $"{keys[0]} or {keys[1]}",
                _ => $"{string.Join(", ", keys[..^1])}, or {keys[^1]}",
            };
        }

        /// <summary>
        /// Gets canonical token label for messages (for example <c>&lt;file-name&gt;</c>).
        /// </summary>
        /// <param name="token">Token instance.</param>
        /// <returns>Display form of the first token name wrapped in angle brackets.</returns>
        internal static string TokenDisplayName(IFormatToken token)
        {
            return $"<{token.Names[0]}>";
        }

        /// <summary>
        /// Validates that no extra formatter argument text is supplied (<see cref="Require"/> precondition).
        /// </summary>
        /// <param name="arg">Raw argument text after <c>:</c>.</param>
        /// <param name="tokenDisplayName">Token label for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when unexpected arguments are present.</exception>
        internal static void RequireNoArgument(string arg, string tokenDisplayName)
        {
            Require.That(string.IsNullOrWhiteSpace(arg), $"{tokenDisplayName} does not accept arguments.", nameof(arg));
        }
    }
}
