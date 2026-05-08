namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Shared helpers for formatter token argument parsing.
    /// </summary>
    internal static class FormatOptionsParsing
    {
        /// <summary>
        /// Throws when <paramref name="arg"/> is not empty or whitespace.
        /// </summary>
        /// <param name="arg">Raw argument text after <c>:</c>.</param>
        /// <param name="tokenDisplayName">Token label for error messages.</param>
        /// <exception cref="InvalidOperationException">Thrown when unexpected arguments are present.</exception>
        internal static void RequireNoArgument(string arg, string tokenDisplayName)
        {
            if (!string.IsNullOrWhiteSpace(arg))
                throw new InvalidOperationException($"{tokenDisplayName} does not accept arguments.");
        }
    }
}
