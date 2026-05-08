using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;full-name&gt;</c> token to the file name including extension.
    /// </summary>
    internal sealed class FullNameToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-name"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            return item.Original.Prefix + item.Original.Extension;
        }
    }
}
