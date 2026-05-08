using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;full-path&gt;</c> token to the full file path.
    /// </summary>
    internal sealed class FullPathToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-path"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            return item.Original.FullPath;
        }
    }
}
