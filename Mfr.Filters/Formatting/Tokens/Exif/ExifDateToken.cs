using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Exif
{
    /// <summary>
    /// Resolves the <c>&lt;exif-date:format&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The argument is a .NET date format string (same idea as <c>&lt;file-date&gt;</c> / <c>&lt;now&gt;</c>).
    /// The pattern is not validated at compile time. Missing DateTaken expands empty.
    /// </para>
    /// </remarks>
    internal sealed class ExifDateToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["exif-date"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format string is missing or blank.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Require.That(
                !string.IsNullOrWhiteSpace(tokenArgs),
                $"{tokenDisplayName} requires a .NET format string argument (for example 'yyyy-MM-dd').",
                nameof(tokenArgs));

            var format = tokenArgs.Trim();
            return item =>
            {
                item.EnsureImagePropertiesLoaded();
                return ExifDataFormatting.FormatDate(item.Original.Exif, format);
            };
        }
    }
}
