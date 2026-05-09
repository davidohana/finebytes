using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-path&gt;</c> token to the full file path.
    /// </summary>
    internal sealed class FullPathToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-path"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item.Original.FullPath;
        }
    }
}
