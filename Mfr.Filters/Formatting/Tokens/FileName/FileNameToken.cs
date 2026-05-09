using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;file-name&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FileNameFormatOptions
    {
        internal static FileNameFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
            return default;
        }
    }

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
            var tokenDisplayName = $"<{Names[0]}>";
            _ = FileNameFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            return item.Original.Prefix;
        }
    }
}
