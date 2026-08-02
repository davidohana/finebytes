using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-or-folder&gt;</c> token to <c>File</c> or <c>Folder</c> from original attributes.
    /// </summary>
    internal sealed class FileOrFolderToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-or-folder"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Original.Attributes.IsDirectory() ? "Folder" : "File";
        }
    }
}
