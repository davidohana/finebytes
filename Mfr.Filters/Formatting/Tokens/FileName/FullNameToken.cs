namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-name&gt;</c> token to the preview <see cref="FileMeta.FullFileName"/>.
    /// </summary>
    [FormatTokenInfo(
        PathFieldLabels.FullFileName,
        PathFieldLabels.FileName,
        "Use full filename name including extension",
        "full-name"
    )]
    internal sealed class FullNameToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-name"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Preview.FullFileName;
        }
    }
}
