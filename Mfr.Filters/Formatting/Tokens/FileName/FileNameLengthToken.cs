using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name-length&gt;</c> token to the character length of the preview full name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full name is preview <see cref="FileMeta.FullFileName"/>. Uses preview so the value tracks predicted renames.
    /// </para>
    /// </remarks>
    [FormatTokenInfo("File Name Length", "File Name", "Character length of the preview full name", "file-name-length")]
    internal sealed class FileNameLengthToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name-length"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item =>
            {
                var preview = item.Preview;
                return preview.FullFileName.Length.ToString(CultureInfo.InvariantCulture);
            };
        }
    }
}
