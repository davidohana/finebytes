using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;file-extension&gt;</c> / <c>&lt;ext&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FileExtensionFormatOptions
    {
        internal static FileExtensionFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
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
            var tokenDisplayName = $"<{Names[0]}>";
            _ = FileExtensionFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            return item.Original.Extension;
        }
    }
}
