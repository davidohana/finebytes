using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-name&gt;</c> token to the file's prefix (no extension).
    /// </summary>
    internal sealed class FileNameToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;file-name&gt;</c> (no parameters).
        /// </summary>
        private readonly record struct FileNameFormatOptions;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = _ParseOptions(arg);
            return item.Original.Prefix;
        }

        private FileNameFormatOptions _ParseOptions(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return default;
        }
    }
}
