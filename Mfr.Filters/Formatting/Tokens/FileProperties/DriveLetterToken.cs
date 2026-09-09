namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;drive-letter&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the drive letter only (e.g. <c>C</c>, no trailing colon) for local paths, or <c>$</c> for UNC paths.
    /// </para>
    /// </remarks>
    [FormatTokenInfo(
        "Drive Letter",
        "File Properties",
        "Use the drive letter on which the file is located",
        "drive-letter"
    )]
    internal sealed class DriveLetterToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["drive-letter"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item =>
            {
                var root = Path.GetPathRoot(item.Original.DirectoryPath) ?? string.Empty;
                if (root.Length == 0)
                {
                    return string.Empty;
                }

                if (root.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    return "$";
                }

                return char.ToUpperInvariant(root[0]).ToString();
            };
        }
    }
}
