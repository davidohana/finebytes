using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.Generators
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
        public Func<RenameItem, string> Compile(string arg)
        {
            var format = string.IsNullOrWhiteSpace(arg) ? null : arg;
            return format is null
                ? _ => DateTimeOffset.UtcNow.ToString("o")
                : _ => DateTimeOffset.UtcNow.ToString(format);
        }
    }
}
