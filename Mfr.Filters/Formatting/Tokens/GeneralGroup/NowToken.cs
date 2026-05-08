using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Parsed arguments for <c>&lt;now&gt;</c>.
    /// </summary>
    /// <param name="Format">
    /// When null or whitespace, emit ISO-8601 UTC; otherwise a .NET date/time format string.
    /// </param>
    internal sealed record NowFormatOptions(string? Format)
    {
        /// <summary>
        /// Parses optional format text (blank uses default UTC ISO output).
        /// </summary>
        /// <param name="arg">Raw argument text.</param>
        internal static NowFormatOptions Parse(string arg)
        {
            return string.IsNullOrWhiteSpace(arg)
                ? new NowFormatOptions(Format: null)
                : new NowFormatOptions(Format: arg);
        }
    }

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
            var options = NowFormatOptions.Parse(arg);
            return options.Format is null
                ? DateTimeOffset.UtcNow.ToString("o")
                : DateTimeOffset.UtcNow.ToString(options.Format);
        }
    }
}
