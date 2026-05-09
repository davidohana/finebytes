using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-extension&gt;</c> and <c>&lt;ext&gt;</c> tokens to the file's extension (with leading dot).
    /// </summary>
    internal sealed class FileExtensionToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;file-extension&gt;</c> / <c>&lt;ext&gt;</c> (no parameters).
        /// </summary>
        private readonly record struct FileExtensionFormatOptions;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-extension", "ext"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = _ParseOptions(arg);
            return item.Original.Extension;
        }

        private FileExtensionFormatOptions _ParseOptions(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return default;
        }
    }
}
