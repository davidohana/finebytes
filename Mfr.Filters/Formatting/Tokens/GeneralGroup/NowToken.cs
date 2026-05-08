using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Resolves the <c>&lt;now&gt;</c> and <c>&lt;now:format&gt;</c> tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the current UTC time as ISO 8601 by default, or with a .NET date/time format string supplied as the argument.
    /// </para>
    /// </remarks>
    internal sealed class NowToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["now"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            return string.IsNullOrWhiteSpace(arg)
                ? DateTimeOffset.UtcNow.ToString("o")
                : DateTimeOffset.UtcNow.ToString(arg);
        }
    }
}
