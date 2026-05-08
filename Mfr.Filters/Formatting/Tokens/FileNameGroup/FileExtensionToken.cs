using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-extension&gt;</c> and <c>&lt;ext&gt;</c> tokens to the file's extension (with leading dot).
    /// </summary>
    internal sealed class FileExtensionToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-extension", "ext"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            return item.Original.Extension;
        }
    }
}
