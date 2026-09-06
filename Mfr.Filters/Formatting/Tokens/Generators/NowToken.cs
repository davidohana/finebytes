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
    [FormatTokenInfo("Now", "General", "Current UTC time (ISO-8601, or custom format)", "now")]
    internal sealed class NowToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["now"];

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            var format = string.IsNullOrWhiteSpace(tokenArgs) ? null : tokenArgs;
            return format is null
                ? _ => DateTimeOffset.UtcNow.ToString("o")
                : _ => DateTimeOffset.UtcNow.ToString(format);
        }
    }
}
