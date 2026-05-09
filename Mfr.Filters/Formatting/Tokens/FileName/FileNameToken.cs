using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name&gt;</c> token to the file's prefix (no extension).
    /// </summary>
    internal sealed class FileNameToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item.Original.Prefix;
        }
    }
}
