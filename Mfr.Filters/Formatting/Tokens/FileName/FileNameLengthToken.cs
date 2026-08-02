using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name-length&gt;</c> token to the character length of the preview full name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full name is preview prefix plus extension. Uses preview so the value tracks predicted renames.
    /// </para>
    /// </remarks>
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
                var length = preview.Prefix.Length + preview.Extension.Length;
                return length.ToString(CultureInfo.InvariantCulture);
            };
        }
    }
}
