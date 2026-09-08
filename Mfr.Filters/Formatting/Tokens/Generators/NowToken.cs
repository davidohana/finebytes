using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.Generators
{
    /// <summary>
    /// Resolves the <c>&lt;now&gt;</c> and <c>&lt;now:format&gt;</c> tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the current UTC time with default format <c>yyyy-MM-dd_HH-mm-ss</c>, or with a .NET
    /// date/time format string supplied as the argument.
    /// </para>
    /// </remarks>
    [FormatTokenInfo(
        "Now",
        "General",
        "Current UTC time (yyyy-MM-dd_HH-mm-ss, or custom format)",
        "now"
    )]
    internal sealed class NowToken : IFormatToken
    {
        private const string DefaultFormat = "yyyy-MM-dd_HH-mm-ss";

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["now"];

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            var format = string.IsNullOrWhiteSpace(tokenArgs) ? DefaultFormat : tokenArgs;
            return _ => DateTimeOffset.UtcNow.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
