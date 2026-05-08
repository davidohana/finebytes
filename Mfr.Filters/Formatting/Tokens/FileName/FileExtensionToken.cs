using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;file-extension&gt;</c> / <c>&lt;ext&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FileExtensionFormatOptions
    {
        internal static FileExtensionFormatOptions Parse(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, "<file-extension>/<ext>");
            return default;
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;file-extension&gt;</c> and <c>&lt;ext&gt;</c> tokens to the file's extension (with leading dot).
    /// </summary>
    internal sealed class FileExtensionToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-extension", "ext"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = FileExtensionFormatOptions.Parse(arg);
            return item.Original.Extension;
        }
    }
}
