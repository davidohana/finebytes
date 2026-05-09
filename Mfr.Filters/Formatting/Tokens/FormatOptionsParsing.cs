using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Shared helpers for formatter token argument parsing.
    /// </summary>
    internal static class FormatOptionsParsing
    {
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
