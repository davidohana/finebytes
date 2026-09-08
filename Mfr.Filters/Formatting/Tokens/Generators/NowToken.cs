using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.Generators
{
    /// <summary>
    /// Defaults for the <c>&lt;now&gt;</c> format token.
    /// </summary>
    public static class NowTokenDefaults
    {
        /// <summary>
        /// .NET format used when <c>&lt;now&gt;</c> has no argument (filename-friendly UTC).
        /// </summary>
        public const string Format = "yyyy-MM-dd_HH-mm-ss";
    }

    /// <summary>
    /// Resolves the <c>&lt;now&gt;</c> and <c>&lt;now:format&gt;</c> tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the current UTC time with default format <see cref="NowTokenDefaults.Format"/>, or with a .NET
    /// date/time format string supplied as the argument.
    /// </para>
    /// </remarks>
    [FormatTokenInfo(
        "Now",
        "General",
        "Current UTC time (" + NowTokenDefaults.Format + ", or custom format)",
        "now"
    )]
    internal sealed class NowToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["now"];

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            var format = string.IsNullOrWhiteSpace(tokenArgs) ? NowTokenDefaults.Format : tokenArgs;
            return _ => DateTimeOffset.UtcNow.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
