using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-name&gt;</c> token to the file's prefix (no extension).
    /// </summary>
    internal sealed class FileNameToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-name"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            return item.Original.Prefix;
        }
    }
}
