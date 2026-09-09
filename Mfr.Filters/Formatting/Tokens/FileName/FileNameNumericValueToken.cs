using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name-numeric-value&gt;</c> token to the first digit run in the preview full name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full name is <see cref="FileMeta.FullFileName"/> on the preview snapshot.
    /// Uses <see cref="FileNameNumericValue.Extract"/> (MFR7: at most 10 digits, zeros stripped via parse).
    /// When the full name has no digits, the token expands to <c>0</c>.
    /// </para>
    /// </remarks>
    [FormatTokenInfo(
        "File Name Numeric Value",
        "File Name",
        "First digit run in the full name (zeros stripped)",
        "file-name-numeric-value"
    )]
    internal sealed class FileNameNumericValueToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name-numeric-value"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => FileNameNumericValue.Extract(item.Preview.FullFileName);
        }
    }
}
